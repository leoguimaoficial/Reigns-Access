using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;
using ReignsAccess.GameData;
using ReignsAccess;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Handles the "Game Over" / "Death" screen.
    /// Identified by the presence of a death message in 'question' text 
    /// and an 'AVANÇAR' button, with no active cards.
    /// </summary>
    /// <summary>
    /// Handles the "Game Over" / "Death" screen.
    /// Identified by the presence of a death message in 'question' text 
    /// and an 'AVANÇAR' button, with no active cards.
    /// </summary>
    public static class DeathScreenNavigator
    {
        private static bool _isActive = false;
        private static bool _allowGameInput = false; // To allow swipe simulation
        private static bool _hasAnnounced = false;
        private static string _lastText = "";
        
        // Navigation items
        private const int ITEM_TEXT = 0;
        private const int ITEM_ADVANCE = 1;
        private static int _currentIndex = 0;
        
        public static bool IsActive => _isActive && !_allowGameInput;

        public static void Update()
        {
            CheckDeathScreen();
        }

        private static void CheckDeathScreen()
        {
            // If we are simulating input, don't interfere with detection state yet
            if (_allowGameInput) return;

            bool wasActive = _isActive;
            string currentText = "";
            bool foundScreen = false;

            var canvas = GameObject.Find("Canvas");
            if (canvas != null && canvas.activeInHierarchy)
            {
                // Check 1: Action button "AVANÇAR" is active
                var touchObj = canvas.transform.Find("touch");
                if (touchObj != null && touchObj.gameObject.activeInHierarchy)
                {
                    var actionTextObj = touchObj.Find("action_touch");
                    if (actionTextObj != null && actionTextObj.gameObject.activeInHierarchy)
                    {
                        var btnText = actionTextObj.GetComponent<Text>();
                        
                        // Relaxed check: Contains "AVANÇAR" or "NEXT" (Case Insensitive)
                        if (btnText != null && btnText.enabled && !string.IsNullOrEmpty(btnText.text))
                        {
                            string t = btnText.text.ToUpper();
                            if (t.Contains("AVANÇAR") || t.Contains("NEXT") || t.Contains("CONTINUE")) 
                            {
                                // Check 2: 'question' text is active
                                var questionObj = canvas.transform.Find("game/question");
                                if (questionObj != null && questionObj.gameObject.activeInHierarchy)
                                {
                                    var text = questionObj.GetComponent<Text>();
                                    if (text != null && text.enabled && !string.IsNullOrEmpty(text.text))
                                    {
                                        foundScreen = true;
                                        currentText = CleanText(text.text);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // CRITICAL CHECK: If any card is active WITH a mask, it is Standard Gameplay or Interactive Narrative.
                // We MUST ABORT in these cases to avoid hijacking the screen (like the Intro).
                if (foundScreen)
                {
                    // Check masks in game/cards (gameplay cards)
                    var cards = canvas.transform.Find("game/cards");
                    if (cards != null)
                    {
                        foreach (Transform child in cards)
                        {
                            if (child.gameObject.activeInHierarchy)
                            {
                                var mask = child.Find("mask");
                                if (mask != null && mask.gameObject.activeInHierarchy)
                                {
                                    // Active Mask = Interactive Card -> Abort Death Screen
                                    foundScreen = false;
                                    break;
                                }
                            }
                        }
                    }
                    
                    // ALSO check masks in game/special (narrative cards like intercaleCard, endCard, etc.)
                    if (foundScreen) // Only check if we haven't already aborted
                    {
                        var special = canvas.transform.Find("game/special");
                        if (special != null)
                        {
                            foreach (Transform child in special)
                            {
                                if (child.gameObject.activeInHierarchy)
                                {
                                    var mask = child.Find("mask");
                                    if (mask != null && mask.gameObject.activeInHierarchy)
                                    {
                                        // Active Mask in special = Narrative Screen -> Abort Death Screen
                                        foundScreen = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _isActive = foundScreen;

            if (_isActive)
            {
                if (!wasActive || currentText != _lastText)
                {
                    _hasAnnounced = false;
                    _lastText = currentText;
                    _currentIndex = 0; // Reset focus to text
                    AnnounceCurrentItem();
                    _hasAnnounced = true;
                }
            }
            else if (wasActive)
            {
                _hasAnnounced = false;
                _lastText = "";
            }
        }

        private static void AnnounceCurrentItem()
        {
            string message = "";
            if (_currentIndex == ITEM_TEXT)
            {
                message = _lastText; // The death message
            }
            else if (_currentIndex == ITEM_ADVANCE)
            {
                string btnLabel = Localization.Get("advance_button");
                if (string.IsNullOrEmpty(btnLabel)) btnLabel = "Avançar";
                message = "Botão: " + btnLabel;
            }

            if (!string.IsNullOrEmpty(message))
            {
                TolkWrapper.Speak(message);
            }
        }

        public static bool HandleInput()
        {
            if (!IsActive) return false;

            // Up/Down navigation
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                _currentIndex--;
                if (_currentIndex < 0) _currentIndex = 1;
                AnnounceCurrentItem();
                return true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                _currentIndex++;
                if (_currentIndex > 1) _currentIndex = 0;
                AnnounceCurrentItem();
                return true;
            }

            // Enter/Space - Advance (Simulate Swipe)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                if (_currentIndex == ITEM_TEXT)
                {
                    AnnounceCurrentItem(); // Repeat text
                }
                else
                {
                    Advance(); // Simulate right swipe
                }
                return true;
            }

            // R - Read current item
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                AnnounceCurrentItem();
                return true;
            }
            
            return true; 
        }

        public static void Advance()
        {
            if (!_isActive) return;

            if (Plugin.Instance != null)
            {
                Plugin.Instance.StartCoroutine(SimulateSwipeInteraction());
            }
            else
            {
                // Fallback
                var anyMb = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
                if (anyMb != null) anyMb.StartCoroutine(SimulateSwipeInteraction());
            }
        }

        // Adapted from NarrativeScreenNavigator logic
        private static System.Collections.IEnumerator SimulateSwipeInteraction()
        {
            // Disable detection to allow input passthrough
            _allowGameInput = true;
            yield return null;

            try
            {
                var gameActs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                MonoBehaviour gameAct = null;
                
                foreach (var mb in gameActs)
                {
                    if (mb.GetType().Name == "GameAct")
                    {
                        gameAct = mb;
                        break;
                    }
                }
                
                if (gameAct != null)
                {
                    var type = gameAct.GetType();
                    var startSlide = type.GetMethod("StartSlide");
                    var updateSlide = type.GetMethod("UpdateSlide");
                    var stopSlide = type.GetMethod("StopSlide");
                    
                    Vector2 swipeRight = new Vector2(500f, 0f);

                    // Touch 1
                    if (startSlide != null) startSlide.Invoke(gameAct, new object[] { swipeRight });
                    
                    for (int i = 0; i < 3; i++)
                    {
                        yield return null;
                        if (updateSlide != null) updateSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    if (stopSlide != null) stopSlide.Invoke(gameAct, null);

                    // Wait between touches
                    for (int i = 0; i < 10; i++) yield return null;

                    // Touch 2
                    if (startSlide != null) startSlide.Invoke(gameAct, new object[] { swipeRight });
                    
                    for (int i = 0; i < 3; i++)
                    {
                        yield return null;
                        if (updateSlide != null) updateSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    if (stopSlide != null) stopSlide.Invoke(gameAct, null);
                }
                else
                {
                }
            }
            finally
            {
                _allowGameInput = false;
            }
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");
            return text.Trim();
        }
    }
}
