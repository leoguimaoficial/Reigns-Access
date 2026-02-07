using System;
using System.Reflection;
using HarmonyLib;
using ReignsAccess.Accessibility;
using UnityEngine;
using UnityEngine.UI;

namespace ReignsAccess.Patches
{
    /// <summary>
    /// Harmony patches for Reigns game classes.
    /// Uses discovered fields: Card.question, Card.override_yes/no, CardAct.yesSign/noSign, GameAct.character
    /// </summary>
    public static class ReignsPatches
    {
        private static Type _cardActType;
        private static Type _characterCardType;
        private static Type _gameActType;
        private static Type _cardType;

        // Cache for GameAct instance to read current state
        private static object _gameActInstance;
        
        // Debounce for ShowDecision to avoid duplicate calls
        private static float _lastShowDecisionTime = 0f;
        private static string _lastShowDecisionOptions = "";
        
        // Armazena o último texto intercalado para a tecla R repetir
        private static string _lastIntercaleText = "";
        
        // Cache field info for performance
        private static FieldInfo _gameActQuestionField;
        private static FieldInfo _gameActCharacterField;
        private static FieldInfo _gameActCardField;
        private static FieldInfo _cardQuestionField;
        private static FieldInfo _cardOverrideYesField;
        private static FieldInfo _cardOverrideNoField;
        private static FieldInfo _cardActYesSignField;
        private static FieldInfo _cardActNoSignField;

        /// <summary>
        /// Initializes and applies patches for Reigns-specific classes.
        /// </summary>
        public static void Initialize(Harmony harmony)
        {
            try
            {
                FindGameTypes();
                CacheFieldInfo();

                if (_cardActType != null)
                    PatchCardAct(harmony);

                if (_characterCardType != null)
                    PatchCharacterCard(harmony);

                if (_gameActType != null)
                    PatchGameAct(harmony);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ReignsPatches.Initialize error: {ex.Message}");
            }
        }

        private static void FindGameTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        switch (type.Name)
                        {
                            case "Card":
                                if (!type.FullName.Contains("System") && !type.FullName.Contains("Cci"))
                                    _cardType = type;
                                break;
                            case "CardAct":
                                _cardActType = type;
                                break;
                            case "CharacterCard":
                                _characterCardType = type;
                                break;
                            case "GameAct":
                                _gameActType = type;
                                break;
                        }
                    }
                }
                catch { }
            }
        }

        private static void CacheFieldInfo()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // GameAct fields
            if (_gameActType != null)
            {
                _gameActQuestionField = _gameActType.GetField("question", flags);
                _gameActCharacterField = _gameActType.GetField("character", flags);
                _gameActCardField = _gameActType.GetField("card", flags);
            }

            // Card fields
            if (_cardType != null)
            {
                _cardQuestionField = _cardType.GetField("question", flags);
                _cardOverrideYesField = _cardType.GetField("override_yes", flags);
                _cardOverrideNoField = _cardType.GetField("override_no", flags);
            }

            // CardAct fields
            if (_cardActType != null)
            {
                _cardActYesSignField = _cardActType.GetField("yesSign", flags);
                _cardActNoSignField = _cardActType.GetField("noSign", flags);
            }
        }

        private static void PatchCardAct(Harmony harmony)
        {
            try
            {
                var showDecisionMethod = _cardActType.GetMethod("ShowDecision",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (showDecisionMethod != null)
                {
                    var postfix = typeof(ReignsPatches).GetMethod(nameof(CardAct_ShowDecision_Postfix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(showDecisionMethod, postfix: new HarmonyMethod(postfix));

                }

                var initCardMethod = _cardActType.GetMethod("InitCard",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (initCardMethod != null)
                {
                    var postfix = typeof(ReignsPatches).GetMethod(nameof(CardAct_InitCard_Postfix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(initCardMethod, postfix: new HarmonyMethod(postfix));

                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"PatchCardAct error: {ex.Message}");
            }
        }

        private static void PatchCharacterCard(Harmony harmony)
        {
            try
            {
                var initMethod = _characterCardType.GetMethod("InitCharacCard",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (initMethod != null)
                {
                    var postfix = typeof(ReignsPatches).GetMethod(nameof(CharacterCard_Init_Postfix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(initMethod, postfix: new HarmonyMethod(postfix));

                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"PatchCharacterCard error: {ex.Message}");
            }
        }

        private static void PatchGameAct(Harmony harmony)
        {
            try
            {
                var showNextMethod = _gameActType.GetMethod("DoShowNextCard",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (showNextMethod != null)
                {
                    var postfix = typeof(ReignsPatches).GetMethod(nameof(GameAct_ShowNextCard_Postfix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(showNextMethod, postfix: new HarmonyMethod(postfix));

                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"PatchGameAct error: {ex.Message}");
            }
        }

        // === Patch Postfix Methods ===

        private static void CardAct_ShowDecision_Postfix(object __instance)
        {
            try
            {
                // Read yes/no signs from CardAct
                string yesText = GetYesNoText(__instance, true);
                string noText = GetYesNoText(__instance, false);
                
                // Create a unique key for this decision
                string optionsKey = $"{yesText}|{noText}";
                float currentTime = Time.time;
                
                // Debounce: ignore if same options within 0.5 seconds
                if (optionsKey == _lastShowDecisionOptions && (currentTime - _lastShowDecisionTime) < 0.5f)
                {
                    return;
                }
                
                _lastShowDecisionOptions = optionsKey;
                _lastShowDecisionTime = currentTime;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"CardAct_ShowDecision_Postfix error: {ex.Message}");
            }
        }

        private static void CardAct_InitCard_Postfix(object __instance)
        {
            // Initialization handled by GameAct
        }

        private static void CharacterCard_Init_Postfix(object __instance)
        {
            // Initialization handled by GameAct
        }

        private static void GameAct_ShowNextCard_Postfix(object __instance)
        {
            try
            {
                _gameActInstance = __instance;
                
                if (__instance is MonoBehaviour mb)
                {
                    mb.StartCoroutine(DelayedGameActRead(__instance));
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GameAct_ShowNextCard_Postfix error: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator DelayedCardRead(object cardActInstance)
        {
            yield return new WaitForSeconds(0.3f);
            
            try
            {
                // Read yesSign and noSign text components
                string yesText = GetYesNoText(cardActInstance, true);
                string noText = GetYesNoText(cardActInstance, false);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"DelayedCardRead error: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator DelayedGameActRead(object gameActInstance)
        {
            yield return new WaitForSeconds(0.5f);
            
            try
            {
                // Primeiro, verificar se há texto intercalado (narrativa entre cartas)
                // GetIntercaleText já retorna o texto limpo de HTML
                string intercaleText = GetIntercaleText();
                if (!string.IsNullOrEmpty(intercaleText))
                {
                    TolkWrapper.Speak(intercaleText);
                    yield break; // Não ler a carta ainda, apenas o texto narrativo
                }

                // Read question text
                string question = "";
                if (_gameActQuestionField != null)
                {
                    var questionText = _gameActQuestionField.GetValue(gameActInstance) as Text;
                    if (questionText != null)
                        question = questionText.text;
                }

                // Read character name
                string character = "";
                if (_gameActCharacterField != null)
                {
                    var characterText = _gameActCharacterField.GetValue(gameActInstance) as Text;
                    if (characterText != null)
                        character = characterText.text;
                }

                // Read card data
                string cardQuestion = "";
                string overrideYes = "";
                string overrideNo = "";
                
                if (_gameActCardField != null)
                {
                    var card = _gameActCardField.GetValue(gameActInstance);
                    if (card != null)
                    {
                        if (_cardQuestionField != null)
                            cardQuestion = _cardQuestionField.GetValue(card) as string ?? "";
                        if (_cardOverrideYesField != null)
                            overrideYes = _cardOverrideYesField.GetValue(card) as string ?? "";
                        if (_cardOverrideNoField != null)
                            overrideNo = _cardOverrideNoField.GetValue(card) as string ?? "";
                    }
                }

                // Auto-announce new card
                string announcement = "";
                
                // Limpar cache de intercaleCard pois agora temos uma carta normal
                _lastIntercaleText = "";
                
                // Use cardQuestion if available (more accurate), otherwise use UI question
                string actualQuestion = !string.IsNullOrEmpty(cardQuestion) ? cardQuestion : question;
                
                // Clean HTML tags from question
                actualQuestion = System.Text.RegularExpressions.Regex.Replace(actualQuestion ?? "", "<.*?>", "");
                
                if (!string.IsNullOrEmpty(character))
                    announcement += character + Core.Localization.Get("character_says");
                
                if (!string.IsNullOrEmpty(actualQuestion))
                    announcement += actualQuestion;

                // Add options - try to get from card overrides or CardAct yesSign/noSign
                string yes = overrideYes;
                string no = overrideNo;
                
                // If no overrides, try to get from CardAct yesSign/noSign
                if (string.IsNullOrEmpty(yes) || string.IsNullOrEmpty(no))
                {
                    // Find CardAct instance
                    object cardActInstance = null;
                    if (_cardActType != null)
                    {
                        var cardActObjects = UnityEngine.Object.FindObjectsOfType(_cardActType as Type);
                        if (cardActObjects != null && cardActObjects.Length > 0)
                            cardActInstance = cardActObjects[0];
                    }
                    
                    if (cardActInstance != null)
                    {
                        if (string.IsNullOrEmpty(yes) && _cardActYesSignField != null)
                        {
                            var yesSign = _cardActYesSignField.GetValue(cardActInstance);
                            if (yesSign != null)
                            {
                                var yesTextField = yesSign.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
                                if (yesTextField != null)
                                    yes = yesTextField.GetValue(yesSign) as string ?? "";
                            }
                        }
                        
                        if (string.IsNullOrEmpty(no) && _cardActNoSignField != null)
                        {
                            var noSign = _cardActNoSignField.GetValue(cardActInstance);
                            if (noSign != null)
                            {
                                var noTextField = noSign.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
                                if (noTextField != null)
                                    no = noTextField.GetValue(noSign) as string ?? "";
                            }
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(announcement))
                {
                    // Only add options if they exist (some cards don't have choices)
                    if (!string.IsNullOrEmpty(yes) && !string.IsNullOrEmpty(no))
                    {
                        announcement += Core.Localization.Get("options_prefix") + yes + Core.Localization.Get("or") + no;
                    }
                    TolkWrapper.Speak(announcement);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"DelayedGameActRead error: {ex.Message}");
            }
        }

        private static string GetYesNoText(object cardActInstance, bool isYes)
        {
            try
            {
                var field = isYes ? _cardActYesSignField : _cardActNoSignField;
                if (field != null)
                {
                    var text = field.GetValue(cardActInstance) as Text;
                    if (text != null)
                        return text.text;
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// Obtém texto intercalado (narrativa que aparece entre cartas).
        /// Retorna o texto já limpo de HTML.
        /// </summary>
        private static string GetIntercaleText()
        {
            try
            {
                var narrativeCards = new string[] { "intercaleCard", "endCard", "effectCard", "objectiveCard" };

                // Procurar pelos GameObjects de narrativa > "mask" > "text"
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    bool isNarrative = false;
                    foreach (var n in narrativeCards) 
                    {
                        if (obj.name == n) { isNarrative = true; break; }
                    }

                    if (isNarrative && obj.activeInHierarchy)
                    {
                        // Procurar o filho "mask"
                        var mask = obj.transform.Find("mask");
                        if (mask != null && mask.gameObject.activeInHierarchy)
                        {
                            // Procurar o Text component
                            var textComp = mask.Find("text");
                            if (textComp != null && textComp.gameObject.activeInHierarchy)
                            {
                                var text = textComp.GetComponent<Text>();
                                if (text != null && text.enabled && !string.IsNullOrWhiteSpace(text.text))
                                {
                                    // Limpar HTML e armazenar
                                    string cleanText = System.Text.RegularExpressions.Regex.Replace(text.text, "<.*?>", "");
                                    _lastIntercaleText = cleanText;
                                    return cleanText;
                                }
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GetIntercaleText error: {ex.Message}");
            }
            
            // Se não encontrou texto ativo, retornar vazio mas NÃO limpar cache
            // O cache só é limpo quando uma nova carta normal aparecer
            return "";
        }

        /// <summary>
        /// Reads current intercale text (narrative). Called by ScreenReader when R is pressed.
        /// Does NOT read normal cards - only intercalated narrative texts.
        /// </summary>
        public static void ReadCurrentCard()
        {
            try
            {
                // Verificar se há texto intercalado ativo
                string intercaleText = GetIntercaleText();
                
                // Se não encontrou ativo, usar cache
                if (string.IsNullOrEmpty(intercaleText) && !string.IsNullOrEmpty(_lastIntercaleText))
                {
                    intercaleText = _lastIntercaleText;
                }
                
                if (!string.IsNullOrEmpty(intercaleText))
                {
                    TolkWrapper.Speak(intercaleText);
                }
                else
                {
                    TolkWrapper.Speak(Core.Localization.Get("no_narrative"));
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ReadCurrentCard error: {ex.Message}");
            }
        }
    }
}
