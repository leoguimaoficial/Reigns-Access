using System;
using System.Collections;
using System.Collections.Generic;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Makes the first Reigns screen accessible. This scene is loaded before the
    /// regular PC scene and has its own Canvas and input controller.
    /// </summary>
    public class DisclaimerScreenNavigator : ScreenNavigatorBase
    {
        private string _contentSignature = "";
        private float _nextRefreshTime;
        private bool _starting;

        public override string ScreenName => Localization.Get("start_screen");

        public override bool IsScreenActive()
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                "disclaimer",
                StringComparison.OrdinalIgnoreCase);
        }

        public override void Update()
        {
            base.Update();

            // DisclaimerAct enables its Canvas shortly after the scene loads.
            // Re-scan briefly so the warning/logo text is not missed because the
            // navigator entered the scene one frame earlier.
            if (!isActive || Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + 0.25f;
            string previousSignature = _contentSignature;
            CollectTexts();
            _contentSignature = string.Join("\n", texts);

            if (_contentSignature != previousSignature && texts.Count > 0)
            {
                currentIndex = 0;
                TolkWrapper.Speak(string.Join(". ", texts), interrupt: true);
            }
        }

        protected override void OnScreenEnter()
        {
            _starting = false;
            _nextRefreshTime = Time.unscaledTime + 0.25f;
            base.OnScreenEnter();
            _contentSignature = string.Join("\n", texts);
        }

        protected override void CollectTexts()
        {
            texts.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var label in UnityEngine.Object.FindObjectsOfType<Text>())
            {
                if (label == null || !label.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string value = ReignsAccess.Navigation.Menus.MenuHelpers.CleanText(label.text);
                if (!string.IsNullOrEmpty(value) && seen.Add(value))
                {
                    texts.Add(value);
                }
            }

            string prompt = Localization.Get("start_screen_prompt");
            if (seen.Add(prompt))
            {
                texts.Add(prompt);
            }
        }

        protected override void ExecuteAction()
        {
            if (_starting)
            {
                return;
            }

            _starting = true;
            TolkWrapper.Speak(Localization.Get("starting_game"), interrupt: true);

            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(StartIfNativeInputDidNot());
                return;
            }

            _starting = false;
            TolkWrapper.Speak(Localization.Get("action_failed"), interrupt: true);
        }

        private IEnumerator StartIfNativeInputDidNot()
        {
            // InputAct sees the same Enter press. Give the game's native input
            // one frame to start DoStart, then provide a fallback only if it did
            // not respond. Calling StartGame twice would launch two scene loads.
            yield return null;

            if (!IsScreenActive())
            {
                yield break;
            }

            try
            {
                var disclaimer = UnityEngine.Object.FindObjectOfType<DisclaimerAct>();
                if (disclaimer != null && disclaimer.loadText != null &&
                    disclaimer.loadText.activeInHierarchy)
                {
                    yield break;
                }

                if (disclaimer != null)
                {
                    disclaimer.StartGame();
                    yield break;
                }

                // Future-version fallback if the controller is renamed but the
                // scene retains its active start button.
                foreach (var button in UnityEngine.Object.FindObjectsOfType<Button>())
                {
                    if (button != null && button.gameObject.activeInHierarchy &&
                        button.enabled && button.interactable)
                    {
                        button.onClick.Invoke();
                        yield break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Could not start the game from the disclaimer screen: {ex}");
            }

            _starting = false;
            TolkWrapper.Speak(Localization.Get("action_failed"), interrupt: true);
        }
    }
}
