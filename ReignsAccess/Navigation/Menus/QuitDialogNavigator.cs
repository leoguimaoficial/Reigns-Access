using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Menus
{
    /// <summary>
    /// Gerencia diálogos de confirmação como "Deseja sair do jogo?"
    /// </summary>
    public static class QuitDialogNavigator
    {
        private static GameObject _dialogPanel;
        private static List<Button> _buttons = new List<Button>();
        private static int _currentButtonIndex = 0;
        private static string _question = "";
        private static bool _isActive = false;

        public static bool IsActive() => _isActive;

        public static void Update()
        {
            CheckForDialog();
        }

        private static void CheckForDialog()
        {
            var dialog = FindActiveDialog();
            if (dialog != null)
            {
                if (_dialogPanel == null || _dialogPanel != dialog)
                {
                    InitializeDialog(dialog);
                }
            }
            else if (_isActive)
            {
                ClearDialog();
            }
        }

        private static GameObject FindActiveDialog()
        {
            // Search by component across every active Canvas. The startup scene
            // stores its confirmation dialog under "Canvas (1)", while gameplay
            // uses "Canvas"; relying on one root made startup quitting silent.
            return UnityEngine.Object.FindObjectsOfType<DialogAct>()
                .Where(candidate => candidate != null && candidate.gameObject.activeInHierarchy)
                .Select(candidate => candidate.gameObject)
                .FirstOrDefault();
        }

        private static void ClearDialog()
        {
            _isActive = false;
            _dialogPanel = null;
            _buttons.Clear();
            _question = "";
            _currentButtonIndex = 0;
        }

        private static void InitializeDialog(GameObject dialog)
        {
            _dialogPanel = dialog;
            _buttons.Clear();
            _currentButtonIndex = 0;

            // Pegar a pergunta
            var questionText = dialog.transform.Find("question")?.GetComponent<Text>();
            _question = MenuHelpers.CleanText(questionText?.text);
            if (string.IsNullOrEmpty(_question))
            {
                _question = Localization.Get("dialog_default");
            }

            // Buscar botões
            var quitBtn = dialog.transform.Find("quit")?.GetComponent<Button>();
            var cancelBtn = dialog.transform.Find("cancel")?.GetComponent<Button>();

            // Adicionar botões na ordem: CANCELAR primeiro, SAIR depois
            if (cancelBtn != null)
            {
                _buttons.Add(cancelBtn);
            }

            if (quitBtn != null)
            {
                _buttons.Add(quitBtn);
            }

            _isActive = _buttons.Count > 0;

            if (_isActive)
            {
                AnnounceDialog();
            }
        }

        private static void AnnounceDialog()
        {
            var buttonNames = string.Join(", ", _buttons.Select(b => GetButtonText(b)));
            var message = _question + Localization.Get("buttons_prefix") + buttonNames + Localization.Get("dialog_nav_hint");
            TolkWrapper.Speak(message, interrupt: true);
            
            // Anunciar botão atual
            AnnounceCurrentButton();
        }

        private static string GetButtonText(Button button)
        {
            var text = button.GetComponentInChildren<Text>();
            var value = MenuHelpers.CleanText(text?.text);
            return string.IsNullOrEmpty(value) ? button.gameObject.name : value;
        }

        private static void AnnounceCurrentButton()
        {
            if (_currentButtonIndex >= 0 && _currentButtonIndex < _buttons.Count)
            {
                var buttonText = GetButtonText(_buttons[_currentButtonIndex]);
                var position = $"{_currentButtonIndex + 1}" + Localization.Get("position_of") + $"{_buttons.Count}";
                TolkWrapper.Speak($"{buttonText}. {position}");
            }
        }

        public static void NavigateLeft()
        {
            if (!_isActive || _buttons.Count == 0) return;

            _currentButtonIndex--;
            if (_currentButtonIndex < 0)
                _currentButtonIndex = _buttons.Count - 1;
            AnnounceCurrentButton();
        }

        public static void NavigateRight()
        {
            if (!_isActive || _buttons.Count == 0) return;

            _currentButtonIndex++;
            if (_currentButtonIndex >= _buttons.Count)
                _currentButtonIndex = 0;
            AnnounceCurrentButton();
        }

        public static void SelectCurrentButton()
        {
            if (!_isActive || _buttons.Count == 0) return;
            if (_currentButtonIndex < 0 || _currentButtonIndex >= _buttons.Count) return;

            var button = _buttons[_currentButtonIndex];
            var buttonText = GetButtonText(button);
            TolkWrapper.Speak(Localization.Get("selected_prefix") + buttonText, interrupt: true);

            // Clicar no botão
            button.onClick.Invoke();
        }

        public static void Close()
        {
            if (!_isActive) return;

            if (_buttons.Count > 0)
            {
                _buttons[0].onClick.Invoke();
            }
        }
    }
}
