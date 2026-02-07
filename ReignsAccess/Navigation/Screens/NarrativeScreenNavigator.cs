using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Handles navigation for narrative/intercalation screens.
    /// These appear between cards with story text and an advance button.
    /// </summary>
    public static class NarrativeScreenNavigator
    {
        private static bool _isActive = false;
        private static bool _hasAnnounced = false;
        private static string _lastText = "";
        private static Button _advanceButton;
        private static GameObject _intercaleCard;
        private static int _currentIndex = 0;
        private const int ITEM_TEXT = 0;
        private const int ITEM_ADVANCE = 1;
        
        // Flag para permitir input do jogo temporariamente durante simulação de swipe
        private static bool _allowGameInput = false;

        public static bool IsActive => _isActive && !_allowGameInput;

        /// <summary>
        /// Update narrative screen state.
        /// </summary>
        public static void Update()
        {
            CheckNarrativeScreen();
        }

        // List of card names that function as narrative screens
        private static readonly string[] _narrativeCardNames = new string[] { "intercaleCard", "endCard", "effectCard", "objectiveCard" };

        private static void CheckNarrativeScreen()
        {
            // Reset reference to search again
            _intercaleCard = null;
            string currentText = "";

            var canvas = GameObject.Find("Canvas");
            if (canvas != null && canvas.activeInHierarchy)
            {
                var special = canvas.transform.Find("game/special");
                if (special != null)
                {
                    // Iterate through all known narrative card types
                    foreach (var cardName in _narrativeCardNames)
                    {
                        var card = special.Find(cardName);
                        if (card != null && card.gameObject.activeInHierarchy)
                        {
                            // Try to get text from mask/text first
                            var textObj = card.Find("mask/text");
                            if (textObj != null && textObj.gameObject.activeInHierarchy)
                            {
                                var text = textObj.GetComponent<Text>();
                                if (text != null && text.enabled && !string.IsNullOrEmpty(text.text))
                                {
                                    _intercaleCard = card.gameObject;
                                    currentText = CleanText(text.text);
                                    break; // Found the active one
                                }
                            }
                            
                            // FALLBACK: If card is active but has no text in mask/text, check game/question
                            // This handles cases where narrative text is in the question field
                            if (_intercaleCard == null)
                            {
                                var questionObj = canvas.transform.Find("game/question");
                                if (questionObj != null && questionObj.gameObject.activeInHierarchy)
                                {
                                    var questionText = questionObj.GetComponent<Text>();
                                    if (questionText != null && questionText.enabled && !string.IsNullOrEmpty(questionText.text))
                                    {
                                        // CRITICAL: Only use question text if the card's mask is ACTIVE
                                        // This ensures it's a narrative screen, not gameplay
                                        var cardMask = card.Find("mask");
                                        if (cardMask != null && cardMask.gameObject.activeInHierarchy)
                                        {
                                            _intercaleCard = card.gameObject;
                                            currentText = CleanText(questionText.text);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            
            // Check if narrative screen is REALLY visible with text
            bool wasActive = _isActive;
            _isActive = (_intercaleCard != null);

            // Just became active OR text changed while active
            // ... (rest of logic remains same, just ensuring scope is correct)
            if (_isActive) 
            {
                if (!wasActive || currentText != _lastText)
                {
                    // SAFETY: Ensure game input is not blocked from a previous failed coroutine
                    if (_allowGameInput)
                    {
                        _allowGameInput = false;
                    }

                    _hasAnnounced = false; // Reset announced flag to force re-read
                    _currentIndex = 0;
                    _lastText = currentText; // Update reference immediately to avoid double announce
                    AnnounceNarrative();
                }
            }
            // Became inactive - reset
            else if (!_isActive && wasActive)
            {
                _hasAnnounced = false;
                _lastText = ""; // Clear last text so next appearance is fresh
            }
        }

        private static void AnnounceNarrative()
        {
            if (_hasAnnounced) return;

            try
            {
                // Find narrative text
                string narrativeText = _lastText; // Use the text we already found and cleaned
                
                // If for some reason _lastText is empty (shouldn't be if we got here), try to fetch again
                if (string.IsNullOrEmpty(narrativeText) && _intercaleCard != null)
                {
                    // Check if it is a standard card (has mask/text)
                    var maskObj = _intercaleCard.transform.Find("mask/text");
                    if (maskObj != null)
                    {
                        var text = maskObj.GetComponent<Text>();
                        if (text != null && !string.IsNullOrEmpty(text.text))
                        {
                            narrativeText = CleanText(text.text);
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(narrativeText))
                    {
                         _lastText = narrativeText;
                    }
                }

                // Find advance button
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    var touchObj = canvas.transform.Find("touch");
                    if (touchObj != null)
                    {
                        var buttonObj = touchObj.Find("but");
                        if (buttonObj != null)
                        {
                            _advanceButton = buttonObj.GetComponent<Button>();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(narrativeText))
                {
                    _lastText = narrativeText;
                    _currentIndex = 0;
                    AnnounceCurrentItem();
                    _hasAnnounced = true;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[NarrativeNav] AnnounceNarrative error: {ex.Message}");
            }
        }

        /// <summary>
        /// Announce the current item in the list.
        /// </summary>
        private static void AnnounceCurrentItem()
        {
            string announcement = "";
            
            if (_currentIndex == ITEM_TEXT)
            {
                announcement = _lastText;
            }
            else if (_currentIndex == ITEM_ADVANCE)
            {
                announcement = Localization.Get("advance_button");
                if (string.IsNullOrEmpty(announcement))
                {
                    announcement = "Avançar";
                }
            }

            TolkWrapper.Speak(announcement);
        }

        /// <summary>
        /// Read the narrative text again.
        /// </summary>
        public static void ReadNarrative()
        {
            if (!_isActive) return;

            if (!string.IsNullOrEmpty(_lastText))
            {
                TolkWrapper.Speak(_lastText);
            }
            else
            {
                AnnounceNarrative();
            }
        }

        /// <summary>
        /// Advance the narrative by simulating two arrow key presses.
        /// </summary>
        public static void Advance()
        {
            if (!_isActive) return;

            try
            {
                // Use Plugin.Instance to start coroutine to ensure it finishes 
                // even if the narrative screen object is disabled/destroyed
                if (Plugin.Instance != null)
                {
                    Plugin.Instance.StartCoroutine(SimulateTwoArrowPresses());
                }
                else
                {
                    // Fallback to random MB if Plugin instance not available (shouldn't happen)
                    var anyMb = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
                    if (anyMb != null)
                    {
                        anyMb.StartCoroutine(SimulateTwoArrowPresses());
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[NarrativeNav] Advance error: {ex.Message}");
            }
        }

        /// <summary>
        /// Coroutine to allow game input and simulate two arrow presses via GameAct.
        /// </summary>
        private static System.Collections.IEnumerator SimulateTwoArrowPresses()
        {
            // Permitir input do jogo (KeyboardNavigator não vai bloquear)
            _allowGameInput = true;
            
            yield return null;
            
            try
            {
                // Procurar GameAct
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
                    var validSlide = type.GetMethod("ValidSlide");
                    var stopSlide = type.GetMethod("StopSlide");
                    
                    Vector2 swipeRight = new Vector2(500f, 0f);
                    
                    // Verificar ValidSlide
                    if (validSlide != null)
                    {
                        var isValid = (bool)validSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    // PRIMEIRO TOQUE
                    if (startSlide != null)
                    {
                        startSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    // Atualizar durante alguns frames
                    for (int i = 0; i < 3; i++)
                    {
                        yield return null;
                        if (updateSlide != null)
                        {
                            updateSlide.Invoke(gameAct, new object[] { swipeRight });
                        }
                    }
                    if (stopSlide != null)
                    {
                        stopSlide.Invoke(gameAct, null);
                    }
                    
                    // Esperar mais tempo entre toques (importante!)
                    for (int i = 0; i < 10; i++)
                    {
                        yield return null;
                    }
                    
                    // Verificar ValidSlide novamente
                    if (validSlide != null)
                    {
                        var isValid = (bool)validSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    // SEGUNDO TOQUE
                    if (startSlide != null)
                    {
                        startSlide.Invoke(gameAct, new object[] { swipeRight });
                    }
                    
                    // Atualizar durante alguns frames
                    for (int i = 0; i < 3; i++)
                    {
                        yield return null;
                        if (updateSlide != null)
                        {
                            updateSlide.Invoke(gameAct, new object[] { swipeRight });
                        }
                    }
                    if (stopSlide != null)
                    {
                        stopSlide.Invoke(gameAct, null);
                    }
                }
                else
                {
                    Plugin.Logger.LogError("[NarrativeScreen] GameAct NÃO ENCONTRADO!");
                }
                
                // Esperar um pouco mais
                yield return null;
                yield return null;
            }
            finally
            {
                // Voltar a bloquear input do jogo
                _allowGameInput = false;
            }
            

        }

        /// <summary>
        /// Coroutine para simular dois toques na seta (apertar duas vezes).
        /// </summary>
        private static System.Collections.IEnumerator SimulateSwipe()
        {
            
            // Desabilitar temporariamente para permitir que o jogo processe o input
            _isActive = false;
            
            // Esperar um frame
            yield return null;
            
            // Procurar GameAct
            var gameActs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in gameActs)
            {
                if (mb.GetType().Name == "GameAct")
                {
                    var type = mb.GetType();
                    var startSlide = type.GetMethod("StartSlide");
                    var stopSlide = type.GetMethod("StopSlide");
                    
                    if (startSlide != null && stopSlide != null)
                    {
                        Vector2 swipeRight = new Vector2(500f, 0f);
                        
                        // PRIMEIRO TOQUE
                        startSlide.Invoke(mb, new object[] { swipeRight });
                        stopSlide.Invoke(mb, null);
                        
                        // Esperar alguns frames entre os toques
                        yield return null;
                        yield return null;
                        yield return null;
                        
                        // SEGUNDO TOQUE
                        startSlide.Invoke(mb, new object[] { swipeRight });
                        stopSlide.Invoke(mb, null);
                    }
                    break;
                }
            }
            
            // Esperar mais alguns frames
            yield return null;
            yield return null;
            
            // Nota: _isActive será redefinido no próximo Update() se ainda estiver em narrativa
        }

        /// <summary>
        /// Clean HTML tags and extra whitespace from text.
        /// </summary>
        private static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Remove HTML tags
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");
            
            // Clean up whitespace
            text = text.Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            return text;
        }

        /// <summary>
        /// Handle keyboard input for narrative screen.
        /// </summary>
        public static bool HandleInput()
        {
            if (!_isActive)
            {
                return false;
            }

            // NAVEGAÇÃO NA LISTA com Up/Down
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

            // Enter - ação no item atual
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                if (_currentIndex == ITEM_TEXT)
                {
                    // No texto, só relê
                    AnnounceCurrentItem();
                }
                else if (_currentIndex == ITEM_ADVANCE)
                {
                    // No botão avançar, avança
                    Advance();
                }
                return true;
            }

            // Space - sempre avança
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                Advance();
                return true;
            }

            // R para reler o item atual
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                AnnounceCurrentItem();
                return true;
            }

            // H for help
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                ReadHelp();
                return true;
            }

            // Q para silenciar
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                TolkWrapper.Silence();
                return true;
            }

            // BLOQUEAR APENAS teclas de stats e opções
            // Deixar left/right passarem para o jogo processar o swipe
            
            // Teclas de leitura de stats - A, S, D, F
            if (UnityEngine.Input.GetKeyDown(KeyCode.A) ||
                UnityEngine.Input.GetKeyDown(KeyCode.S) ||
                UnityEngine.Input.GetKeyDown(KeyCode.D) ||
                UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                return true;
            }

            // Teclas de opções - E, T, I, O
            if (UnityEngine.Input.GetKeyDown(KeyCode.E) ||
                UnityEngine.Input.GetKeyDown(KeyCode.T) ||
                UnityEngine.Input.GetKeyDown(KeyCode.I) ||
                UnityEngine.Input.GetKeyDown(KeyCode.O))
            {
                return true;
            }

            return false;
        }

        private static void ReadHelp()
        {
            string help = Localization.Get("narrative_help");
            if (string.IsNullOrEmpty(help))
            {
                help = "Narrative screen. R to read text. Enter or Space to advance.";
            }
            TolkWrapper.Speak(help);
        }
    }
}
