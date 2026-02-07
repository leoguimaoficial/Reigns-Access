using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ReignsAccess.Navigation.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReignsAccess.Accessibility
{
    /// <summary>
    /// Monitors UI changes and reads screen text automatically.
    /// Useful for title screens, menus, and any UI with text.
    /// </summary>
    public class ScreenReader : MonoBehaviour
    {
        private static ScreenReader _instance;
        private string _lastAnnouncedText = "";
        private float _lastCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.5f; // Check every half second for dialogs
        private bool _hasAnnouncedScene = false;
        private string _currentScene = "";
        private bool _dialogWasActive = false;
        private string _lastDialogText = "";

        public static void Create(GameObject parent)
        {
            if (_instance == null)
            {
                _instance = parent.AddComponent<ScreenReader>();
            }
        }

        public static void Destroy()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
            }
        }

        private void Start()
        {
            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Don't do automatic initial scan - user will press keys to read
            // StartCoroutine(InitialScan());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private IEnumerator InitialScan()
        {
            // Wait for game to initialize
            yield return new WaitForSeconds(1.0f);
            
            // Disabled - user controls reading with arrow keys
            // ScanAndAnnounceScreen("Initial scan");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _currentScene = scene.name;
            _hasAnnouncedScene = false;
            
            // Just announce scene name, not full scan
            if (scene.name == "reigns_pc")
            {
                TolkWrapper.Speak(Core.Localization.Get("game_loaded"));
            }
            else
            {
                TolkWrapper.Speak(Core.Localization.Get("screen_prefix") + scene.name);
            }
        }

        private IEnumerator DelayedSceneAnnouncement(string sceneName)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Disabled automatic scan
            if (!_hasAnnouncedScene)
            {
                _hasAnnouncedScene = true;
                ScanAndAnnounceScreen($"Scene: {sceneName}");
            }
        }

        private void Update()
        {
            // Periodic check for dialog changes
            if (Time.time - _lastCheckTime > CHECK_INTERVAL)
            {
                _lastCheckTime = Time.time;
                CheckForDialogs();
            }
        }

        /// <summary>
        /// Checks if a dialog has appeared and reads it automatically.
        /// </summary>
        private void CheckForDialogs()
        {
            bool dialogActive = IsDialogActive();
            
            if (dialogActive && !_dialogWasActive)
            {
                // Dialog just appeared - read it!
                StartCoroutine(ReadDialogContent());
            }
            else if (!dialogActive && _dialogWasActive)
            {
                // Dialog closed
                _lastDialogText = "";
            }
            
            _dialogWasActive = dialogActive;
        }

        /// <summary>
        /// Checks if any dialog is currently active.
        /// </summary>
        private bool IsDialogActive()
        {
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.activeInHierarchy)
                {
                    string name = obj.name.ToLower();
                    // Dialog indicators from log: dialog(Clone), DialogAct
                    if ((name.Contains("dialog") && name.Contains("clone")) ||
                        obj.GetComponent("DialogAct") != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Reads dialog content (question + buttons).
        /// </summary>
        private IEnumerator ReadDialogContent()
        {
            // Small delay to let dialog fully render
            yield return new WaitForSeconds(0.1f);
            
            var dialogParts = new List<string>();
            
            // Find the question text in the dialog
            var uiTexts = FindObjectsOfType<UnityEngine.UI.Text>();
            foreach (var text in uiTexts)
            {
                if (text.gameObject.activeInHierarchy && 
                    !string.IsNullOrWhiteSpace(text.text))
                {
                    string objName = text.gameObject.name.ToLower();
                    
                    // Prioritize question/title text
                    if (objName == "question" || objName == "title" || objName == "message")
                    {
                        string cleanText = CleanText(text.text);
                        if (!string.IsNullOrEmpty(cleanText))
                        {
                            dialogParts.Insert(0, cleanText); // Put question first
                        }
                    }
                }
            }
            
            // Find button labels
            var buttons = FindObjectsOfType<UnityEngine.UI.Button>();
            var buttonLabels = new List<string>();
            foreach (var btn in buttons)
            {
                if (btn.gameObject.activeInHierarchy && btn.interactable)
                {
                    // Check if this button is part of a dialog
                    var parent = btn.transform.parent;
                    bool isDialogButton = false;
                    while (parent != null)
                    {
                        if (parent.name.ToLower().Contains("dialog"))
                        {
                            isDialogButton = true;
                            break;
                        }
                        parent = parent.parent;
                    }
                    
                    if (isDialogButton || btn.gameObject.name.ToLower().Contains("cancel") || 
                        btn.gameObject.name.ToLower().Contains("quit") ||
                        btn.gameObject.name.ToLower().Contains("yes") ||
                        btn.gameObject.name.ToLower().Contains("no"))
                    {
                        string label = GetButtonLabel(btn);
                        if (!string.IsNullOrEmpty(label) && !buttonLabels.Contains(label))
                        {
                            buttonLabels.Add(label);
                        }
                    }
                }
            }
            
            // Add button options
            if (buttonLabels.Count > 0)
            {
                dialogParts.Add("Opções: " + string.Join(", ", buttonLabels));
                dialogParts.Add("Use setas para navegar e Enter para selecionar");
            }
            
            // Announce if we have content
            if (dialogParts.Count > 0)
            {
                string announcement = string.Join(". ", dialogParts);
                
                if (announcement != _lastDialogText)
                {
                    _lastDialogText = announcement;
                    TolkWrapper.Speak(announcement);
                    
                }
            }
        }

        /// <summary>
        /// Gets the text label of a button.
        /// </summary>
        private string GetButtonLabel(UnityEngine.UI.Button btn)
        {
            // Try to get text from child Text component
            var text = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null && !string.IsNullOrWhiteSpace(text.text))
            {
                return CleanText(text.text);
            }
            
            // Try sibling with same name (Reigns pattern: button "quit" has sibling Text "quit")
            var parent = btn.transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    if (child.name == btn.gameObject.name)
                    {
                        var siblingText = child.GetComponent<UnityEngine.UI.Text>();
                        if (siblingText != null && !string.IsNullOrWhiteSpace(siblingText.text))
                        {
                            return CleanText(siblingText.text);
                        }
                    }
                }
            }
            
            // Fall back to button name
            return btn.gameObject.name;
        }

        /// <summary>
        /// Manually trigger a screen scan (called by keyboard navigator).
        /// </summary>
        public static void ScanScreen()
        {
            if (_instance != null)
            {
                _instance.ScanAndAnnounceScreen("Manual scan");
            }
        }

        /// <summary>
        /// Scans all visible UI text and announces it.
        /// </summary>
        private void ScanAndAnnounceScreen(string source)
        {
            try
            {
                var allTexts = new List<string>();
                
                // Find all UI.Text components
                var uiTexts = FindObjectsOfType<UnityEngine.UI.Text>();
                foreach (var text in uiTexts)
                {
                    if (text.gameObject.activeInHierarchy && 
                        !string.IsNullOrWhiteSpace(text.text) &&
                        IsTextVisible(text.gameObject))
                    {
                        string cleanText = CleanText(text.text);
                        if (!string.IsNullOrEmpty(cleanText) && !allTexts.Contains(cleanText))
                        {
                            allTexts.Add(cleanText);
                            
                        }
                    }
                }

                // Find all TextMeshPro components
                var tmpTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>();
                foreach (var text in tmpTexts)
                {
                    if (text.gameObject.activeInHierarchy && 
                        !string.IsNullOrWhiteSpace(text.text) &&
                        IsTextVisible(text.gameObject))
                    {
                        string cleanText = CleanText(text.text);
                        if (!string.IsNullOrEmpty(cleanText) && !allTexts.Contains(cleanText))
                        {
                            allTexts.Add(cleanText);
                            
                        }
                    }
                }

                // Also check for TextMeshPro (3D text)
                var tmp3DTexts = FindObjectsOfType<TMPro.TextMeshPro>();
                foreach (var text in tmp3DTexts)
                {
                    if (text.gameObject.activeInHierarchy && 
                        !string.IsNullOrWhiteSpace(text.text))
                    {
                        string cleanText = CleanText(text.text);
                        if (!string.IsNullOrEmpty(cleanText) && !allTexts.Contains(cleanText))
                        {
                            allTexts.Add(cleanText);
                            
                        }
                    }
                }

                // Build announcement
                if (allTexts.Count > 0)
                {
                    string announcement = string.Join(". ", allTexts);
                    
                    // Only announce if different from last
                    if (announcement != _lastAnnouncedText)
                    {
                        _lastAnnouncedText = announcement;
                        TolkWrapper.Speak(announcement);
                    }
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ScanAndAnnounceScreen error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a GameObject is likely visible on screen.
        /// </summary>
        private bool IsTextVisible(GameObject go)
        {
            // Check if any parent has CanvasGroup with alpha 0
            var canvasGroups = go.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in canvasGroups)
            {
                if (cg.alpha < 0.1f)
                    return false;
            }

            // Check if object has renderer and is enabled
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && !renderer.enabled)
                return false;

            return true;
        }

        /// <summary>
        /// Cleans up text for speech.
        /// </summary>
        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Remove common formatting
            text = text.Replace("\n", " ").Replace("\r", " ");
            text = text.Replace("<br>", " ").Replace("<BR>", " ");
            
            // Remove rich text tags
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "");
            
            // Clean up whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            // Skip very short or placeholder text
            if (text.Length < 2)
                return "";
            if (text == "..." || text == "-")
                return "";

            return text;
        }

    }
}
