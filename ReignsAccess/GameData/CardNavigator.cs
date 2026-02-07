using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.GameData
{
    /// <summary>
    /// Handles card navigation and selection for accessibility.
    /// Works like a simple menu with two options (left/right).
    /// Arrow keys navigate between options, Enter executes.
    /// </summary>
    public static class CardNavigator
    {
        // Selection state
        private static CardSelection _currentSelection = CardSelection.None;
        
        // Duplo clique: armazena quando a última tecla foi pressionada
        private static float _lastLeftPressTime = 0f;
        private static float _lastRightPressTime = 0f;
        private const float DOUBLE_PRESS_THRESHOLD = 0.8f; // 800ms para considerar duplo clique
        
        // Cached types and fields for card manipulation
        private static Type _gameActType;
        private static Type _cardActType;
        private static object _gameActInstance;
        private static FieldInfo _cardScField;
        private static FieldInfo _decisionField;

        public enum CardSelection
        {
            None,
            Left,   // No option
            Right   // Yes option
        }

        /// <summary>
        /// Get current selection.
        /// </summary>
        public static CardSelection CurrentSelection => _currentSelection;

        /// <summary>
        /// Initialize cached types for card manipulation.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "GameAct")
                            _gameActType = type;
                        else if (type.Name == "CardAct" && !type.FullName.Contains("System"))
                            _cardActType = type;
                    }
                }

                if (_gameActType != null)
                {
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    _cardScField = _gameActType.GetField("cardSc", flags);
                    _decisionField = _cardActType?.GetField("decision", flags);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"CardNavigator.Initialize error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current GameAct instance.
        /// </summary>
        private static object GetGameActInstance()
        {
            if (_gameActType == null) return null;
            
            try
            {
                // Find GameAct in scene
                var gameActs = UnityEngine.Object.FindObjectsOfType(_gameActType);
                if (gameActs.Length > 0)
                {
                    _gameActInstance = gameActs.GetValue(0);
                    return _gameActInstance;
                }
            }
            catch { }
            
            return null;
        }

        /// <summary>
        /// Swipe left directly (No option) - reads the option then executes.
        /// </summary>
        public static void SwipeLeft()
        {
            string optionText = GetOptionText(false);
            TolkWrapper.Speak(optionText);
            SwipeCard(false);
        }

        /// <summary>
        /// Swipe right directly (Yes option) - reads the option then executes.
        /// </summary>
        public static void SwipeRight()
        {
            string optionText = GetOptionText(true);
            TolkWrapper.Speak(optionText);
            SwipeCard(true);
        }

        /// <summary>
        /// Select the right option (Yes/Sim).
        /// </summary>
        public static void SelectRight()
        {
            float currentTime = Time.time;
            
            // Se já está selecionado direita E foi pressionado rapidamente, executar
            if (_currentSelection == CardSelection.Right && 
                (currentTime - _lastRightPressTime) < DOUBLE_PRESS_THRESHOLD)
            {
                ExecuteSelection();
                _lastRightPressTime = 0f; // Reset para evitar triplo clique
                return;
            }

            _currentSelection = CardSelection.Right;
            _lastRightPressTime = currentTime;
            string optionText = GetOptionText(true);
            TolkWrapper.Speak(optionText);
        }

        /// <summary>
        /// Select the left option (No/Não).
        /// </summary>
        public static void SelectLeft()
        {
            float currentTime = Time.time;
            
            // Se já está selecionado esquerda E foi pressionado rapidamente, executar
            if (_currentSelection == CardSelection.Left && 
                (currentTime - _lastLeftPressTime) < DOUBLE_PRESS_THRESHOLD)
            {
                ExecuteSelection();
                _lastLeftPressTime = 0f; // Reset para evitar triplo clique
                return;
            }

            _currentSelection = CardSelection.Left;
            _lastLeftPressTime = currentTime;
            string optionText = GetOptionText(false);
            TolkWrapper.Speak(optionText);
        }

        /// <summary>
        /// Execute the current selection (swipe the card).
        /// </summary>
        public static bool ExecuteSelection()
        {
            if (_currentSelection == CardSelection.None)
            {
                TolkWrapper.Speak(Localization.Get("select_option_hint"));
                return false;
            }

            // Execute the swipe
            bool success = SwipeCard(_currentSelection == CardSelection.Right);
            
            if (success)
            {
                string optionText = GetOptionText(_currentSelection == CardSelection.Right);
                TolkWrapper.Speak(Localization.Get("chosen_prefix") + optionText);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("action_failed"));
            }

            // Reset for next card
            ResetSelection();
            
            return success;
        }

        /// <summary>
        /// Reset the current selection.
        /// </summary>
        public static void ResetSelection()
        {
            _currentSelection = CardSelection.None;
        }

        /// <summary>
        /// Get the current selection.
        /// </summary>
        public static CardSelection GetCurrentSelection()
        {
            return _currentSelection;
        }

        /// <summary>
        /// Get the option text (without "Direita:" or "Esquerda:" prefix).
        /// Uses Card's override_yes/override_no fields for accurate text.
        /// </summary>
        private static string GetOptionText(bool isRight)
        {
            try
            {
                // Method 1: Read from GameAct.card (most reliable)
                var gameActs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in gameActs)
                {
                    if (mb.GetType().Name == "GameAct")
                    {
                        var cardField = mb.GetType().GetField("card", 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (cardField != null)
                        {
                            var card = cardField.GetValue(mb);
                            if (card != null)
                            {
                                string fieldName = isRight ? "override_yes" : "override_no";
                                var overrideField = card.GetType().GetField(fieldName,
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (overrideField != null)
                                {
                                    string overrideText = overrideField.GetValue(card) as string;
                                    if (!string.IsNullOrWhiteSpace(overrideText))
                                    {
                                        return CleanHtmlTags(overrideText);
                                    }
                                }
                            }
                        }
                        break;
                    }
                }

                // Method 2: Try to find CardAct and read yesSign/noSign
                foreach (var mb in gameActs)
                {
                    if (mb.GetType().Name == "CardAct" && mb.gameObject.activeInHierarchy)
                    {
                        string fieldName = isRight ? "yesSign" : "noSign";
                        var signField = mb.GetType().GetField(fieldName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (signField != null)
                        {
                            var textObj = signField.GetValue(mb) as Text;
                            if (textObj != null && !string.IsNullOrWhiteSpace(textObj.text))
                            {
                                string text = textObj.text;
                                // Skip if it's just "yes" or "no" defaults
                                if (!text.Equals("yes", StringComparison.OrdinalIgnoreCase) &&
                                    !text.Equals("no", StringComparison.OrdinalIgnoreCase))
                                {
                                    return CleanHtmlTags(text);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GetOptionText error: {ex.Message}");
            }
            
            // Fallback: search for yesSign/noSign UI elements
            try
            {
                var signs = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
                foreach (var sign in signs)
                {
                    if (sign.gameObject.name.Contains(isRight ? "yes" : "no") && 
                        sign.gameObject.activeInHierarchy &&
                        !string.IsNullOrEmpty(sign.text))
                    {
                        return CleanHtmlTags(sign.text);
                    }
                }
            }
            catch { }
            
            return ""; // Return empty if no text found
        }

        /// <summary>
        /// Remove HTML tags from text.
        /// </summary>
        private static string CleanHtmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "").Trim();
        }

        /// <summary>
        /// Simulate swiping the card left or right.
        /// </summary>
        private static bool SwipeCard(bool swipeRight)
        {
            try
            {
                // Method 1: Try to find and manipulate CharacterCard directly
                var characterCards = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in characterCards)
                {
                    if (mb.GetType().Name == "CharacterCard" && mb.gameObject.activeInHierarchy)
                    {
                        // Check if this card is in the center (active card)
                        var rectTransform = mb.GetComponent<RectTransform>();
                        if (rectTransform != null && Mathf.Abs(rectTransform.anchoredPosition.x) < 100)
                        {
                            // This is likely the active card
                            // Try to set decision and trigger choice
                            var decisionField = mb.GetType().GetField("decision", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (decisionField != null)
                            {
                                int decision = swipeRight ? 1 : -1;
                                decisionField.SetValue(mb, decision);
                                
                                SimulateCardSwipe(rectTransform, swipeRight);
                                
                                return true;
                            }
                        }
                    }
                }

                // Method 2: Simulate mouse drag
                return SimulateMouseSwipe(swipeRight);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"SwipeCard error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Simulate card swipe by moving the card.
        /// </summary>
        private static void SimulateCardSwipe(RectTransform cardTransform, bool swipeRight)
        {
            try
            {
                // Get the card's MonoBehaviour to start a coroutine
                var mb = cardTransform.GetComponent<MonoBehaviour>();
                if (mb != null)
                {
                    mb.StartCoroutine(AnimateSwipe(cardTransform, swipeRight));
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"SimulateCardSwipe error: {ex.Message}");
            }
        }

        /// <summary>
        /// Animate the card swipe.
        /// </summary>
        private static System.Collections.IEnumerator AnimateSwipe(RectTransform cardTransform, bool swipeRight)
        {
            float targetX = swipeRight ? 800f : -800f;
            float startX = cardTransform.anchoredPosition.x;
            float duration = 0.3f;
            float elapsed = 0f;

            // Get the CharacterCard component
            var cardComponent = cardTransform.GetComponent<MonoBehaviour>();
            
            // IMPORTANT: Set grabbed = true and timeSinceGrab to simulate proper drag
            if (cardComponent != null)
            {
                var grabbedField = cardComponent.GetType().GetField("grabbed",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (grabbedField != null)
                {
                    grabbedField.SetValue(cardComponent, true);
                }
                
                var timeSinceGrabField = cardComponent.GetType().GetField("timeSinceGrab",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (timeSinceGrabField != null)
                {
                    timeSinceGrabField.SetValue(cardComponent, 0.5f);
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float newX = Mathf.Lerp(startX, targetX, t);
                cardTransform.anchoredPosition = new Vector2(newX, cardTransform.anchoredPosition.y);
                yield return null;
            }

            // Final position
            cardTransform.anchoredPosition = new Vector2(targetX, cardTransform.anchoredPosition.y);
            
            if (cardComponent != null)
            {
                try
                {
                    // Set grabbed = false to trigger the release logic in Update()
                    var grabbedField = cardComponent.GetType().GetField("grabbed",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (grabbedField != null)
                    {
                        grabbedField.SetValue(cardComponent, false);
                    }
                    
                    // Try to find and call ConfirmChoice or similar method
                    var confirmMethod = cardComponent.GetType().GetMethod("ConfirmChoice",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (confirmMethod != null)
                    {
                        confirmMethod.Invoke(cardComponent, null);
                    }
                    else
                    {
                        // Try DoConfirmChoice
                        var doConfirmMethod = cardComponent.GetType().GetMethod("DoConfirmChoice",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (doConfirmMethod != null)
                        {
                            doConfirmMethod.Invoke(cardComponent, null);
                        }
                        else
                        {
                            // Try ValidateChoice
                            var validateMethod = cardComponent.GetType().GetMethod("ValidateChoice",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (validateMethod != null)
                            {
                                validateMethod.Invoke(cardComponent, null);
                            }
                            else
                            {
                                // Choice methods not found - the game will process via Update()
                                // This is expected behavior in some game versions
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[CardNav] Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Fallback: simulate mouse swipe.
        /// </summary>
        private static bool SimulateMouseSwipe(bool swipeRight)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"SimulateMouseSwipe error: {ex.Message}");
                return false;
            }
        }
    }
}
