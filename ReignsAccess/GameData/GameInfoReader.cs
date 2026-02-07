using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.GameData
{
    /// <summary>
    /// Reads specific game information by finding UI elements by name.
    /// Provides organized access to card, character, stats, and king info.
    /// </summary>
    public static class GameInfoReader
    {
        // Cache for commonly accessed UI elements
        private static Dictionary<string, Text> _textCache = new Dictionary<string, Text>();
        private static float _lastCacheTime = 0f;
        private const float CACHE_DURATION = 0.5f;

        /// <summary>
        /// Reads the current card info: Character name and question only.
        /// Called with Up Arrow.
        /// </summary>
        public static void ReadCard()
        {
            RefreshCacheIfNeeded();

            string character = GetTextByName("who");
            string question = GetTextByName("question");

            // Clean up rich text tags from question
            question = CleanRichText(question);

            string announcement = "";

            if (!string.IsNullOrEmpty(character))
            {
                announcement = character;
            }

            if (!string.IsNullOrEmpty(question))
            {
                if (!string.IsNullOrEmpty(announcement))
                    announcement += " diz: ";
                announcement += question;
            }

            if (!string.IsNullOrEmpty(announcement))
            {
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("no_card"));
            }
        }

        /// <summary>
        /// Reads the card options (left and right) - without stat changes.
        /// Called with E key.
        /// </summary>
        public static void ReadOptions()
        {
            RefreshCacheIfNeeded();

            string yesOption = GetTextByName("yessign");
            string noOption = GetTextByName("nosign");

            string yes = !string.IsNullOrEmpty(yesOption) ? yesOption : Localization.Get("yes_default");
            string no = !string.IsNullOrEmpty(noOption) ? noOption : Localization.Get("no_default");

            // Clean HTML tags
            yes = CleanRichText(yes);
            no = CleanRichText(no);

            string announcement = Localization.Get("right_prefix") + yes + Localization.Get("left_prefix") + no;
            TolkWrapper.Speak(announcement);
        }

        /// <summary>
        /// Reads which stats will be affected by the current card choices.
        /// Called with T key.
        /// </summary>
        public static void ReadAffectedStats()
        {
            RefreshCacheIfNeeded();

            string changes = GetStatChanges("");
            
            if (!string.IsNullOrEmpty(changes))
            {
                TolkWrapper.Speak(changes);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("no_stats_affected"));
            }
        }

        /// <summary>
        /// Reads a single stat by name.
        /// Called with A, S, D, F keys.
        /// </summary>
        public static void ReadStat(string statName, string statLabel)
        {
            RefreshCacheIfNeeded();

            int value = GetStatValue(statName);
            if (value >= 0)
            {
                string announcement = $"{statLabel}: {value}%";
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(statLabel + Localization.Get("stat_not_available"));
            }
        }

        /// <summary>
        /// Reads the kingdom statistics.
        /// Called with Down Arrow.
        /// </summary>
        public static void ReadStats()
        {
            RefreshCacheIfNeeded();

            // Find the DataAnim components which contain the stat gauges
            var stats = new List<string>();

            // The stats are: spiritual (Igreja), demography (Povo), military (Exército), treasure (Tesouro)
            string[] statNames = { "spiritual", "demography", "military", "treasure" };
            string[] statLabels = { 
                Localization.Get("stat_church"), 
                Localization.Get("stat_people"), 
                Localization.Get("stat_army"), 
                Localization.Get("stat_treasury") 
            };

            for (int i = 0; i < statNames.Length; i++)
            {
                int value = GetStatValue(statNames[i]);
                if (value >= 0)
                {
                    stats.Add($"{statLabels[i]}: {value}%");
                }
            }

            if (stats.Count > 0)
            {
                string announcement = string.Join(". ", stats);
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("stats_not_available"));
            }
        }

        /// <summary>
        /// Reads the right option (Yes/Swipe Right).
        /// Called with Right Arrow.
        /// </summary>
        public static void ReadRightOption()
        {
            RefreshCacheIfNeeded();

            string yesOption = GetTextByName("yessign");
            if (!string.IsNullOrEmpty(yesOption))
            {
                // Also try to read what stats will be affected
                string changes = GetStatChanges("right");
                string announcement = yesOption;
                if (!string.IsNullOrEmpty(changes))
                    announcement += $". {changes}";
                
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("yes_default"));
            }
        }

        /// <summary>
        /// Reads the left option (No/Swipe Left).
        /// Called with Left Arrow.
        /// </summary>
        public static void ReadLeftOption()
        {
            RefreshCacheIfNeeded();

            string noOption = GetTextByName("nosign");
            if (!string.IsNullOrEmpty(noOption))
            {
                string changes = GetStatChanges("left");
                string announcement = noOption;
                if (!string.IsNullOrEmpty(changes))
                    announcement += $". {changes}";
                
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("no_default"));
            }
        }

        /// <summary>
        /// Reads king/reign information.
        /// Called with I key.
        /// </summary>
        public static void ReadKingInfo()
        {
            RefreshCacheIfNeeded();

            string king = GetTextByName("king");
            string year = GetTextByName("year");
            string age = GetTextByName("age"); // Anos no poder (número)
            string yearInPowerLabel = GetTextByName("year_in_power"); // Texto "anos no poder"

            string announcement = "";

            if (!string.IsNullOrEmpty(king))
                announcement = Localization.Get("king_info_prefix") + king;

            if (!string.IsNullOrEmpty(year))
                announcement += Localization.Get("year_info_prefix") + year;

            // Adiciona quantos anos no poder
            if (!string.IsNullOrEmpty(age))
            {
                announcement += $". {age}" + Localization.Get("years_in_power");
            }

            if (!string.IsNullOrEmpty(announcement))
            {
                TolkWrapper.Speak(announcement);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("king_info_error"));
            }
        }

        /// <summary>
        /// Reads the current objective.
        /// Called with O key.
        /// </summary>
        public static void ReadObjective()
        {
            RefreshCacheIfNeeded();

            string objective = GetTextByName("objective");

            if (!string.IsNullOrEmpty(objective))
            {
                TolkWrapper.Speak(Localization.Get("objective_prefix") + objective);
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("no_objective"));
            }
        }

        /// <summary>
        /// Reads help information about available keys.
        /// Called with H key.
        /// </summary>
        public static void ReadHelp()
        {
            string help = Localization.Get("help_intro") +
                Localization.Get("help_arrows") +
                Localization.Get("help_swipe") +
                Localization.Get("help_options") +
                Localization.Get("help_stats") +
                Localization.Get("help_info") +
                Localization.Get("help_menu") +
                Localization.Get("help_general");
            
            TolkWrapper.Speak(help);
        }

        // === Helper Methods ===

        private static void RefreshCacheIfNeeded()
        {
            if (Time.time - _lastCacheTime > CACHE_DURATION)
            {
                _textCache.Clear();
                _lastCacheTime = Time.time;

                // Find all Text components and cache by name
                var allTexts = UnityEngine.Object.FindObjectsOfType<Text>();
                foreach (var text in allTexts)
                {
                    if (text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text))
                    {
                        string name = text.gameObject.name.ToLower();
                        if (!_textCache.ContainsKey(name))
                        {
                            _textCache[name] = text;
                        }
                    }
                }
            }
        }

        private static string GetTextByName(string name)
        {
            name = name.ToLower();

            // STRATEGY 1: Search within active card (Most accurate)
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var cards = canvas.transform.Find("game/cards");
                if (cards != null)
                {
                    foreach (Transform card in cards)
                    {
                        if (card.gameObject.activeInHierarchy)
                        {
                            // Search recursively in this active card for the named text
                            var foundText = FindTextRecursively(card, name);
                            if (foundText != null && !string.IsNullOrEmpty(foundText.text))
                            {
                                return foundText.text;
                            }
                            // If we found the active card but not the text, we might stop here? 
                            // No, let's fall back just in case the structure is weird.
                            break; 
                        }
                    }
                }
            }

            // STRATEGY 2: Cache (Fast but potentially stale)
            if (_textCache.TryGetValue(name, out Text text))
            {
                if (text != null && text.gameObject.activeInHierarchy)
                    return text.text;
            }

            // STRATEGY 3: Global search (Fallback)
            var allTexts = UnityEngine.Object.FindObjectsOfType<Text>();
            foreach (var t in allTexts)
            {
                if (t.gameObject.activeInHierarchy && 
                    t.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(t.text))
                {
                    return t.text;
                }
            }

            return "";
        }

        private static Text FindTextRecursively(Transform parent, string name)
        {
            // Check direct children first? Or check parent itself?
            // name check
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                var t = parent.GetComponent<Text>();
                if (t != null && t.enabled) return t;
            }

            foreach (Transform child in parent)
            {
                var result = FindTextRecursively(child, name);
                if (result != null) return result;
            }

            return null;
        }

        private static int GetStatValue(string statName)
        {
            // Find the stat GameObject by name (spiritual, demography, military, treasure)
            // The actual value is in the DataAnim component's 'dataReal' field (0-100 scale)
            // NOT in the 'amount' Text (which always shows '20') or gauge.fillAmount
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Equals(statName, StringComparison.OrdinalIgnoreCase) && obj.activeInHierarchy)
                {
                    // Get the DataAnim component and read dataReal via reflection
                    var components = obj.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        var compType = comp.GetType();
                        if (compType.Name == "DataAnim")
                        {
                            // Read the dataReal field (0-100 scale, already percentage!)
                            var dataRealField = compType.GetField("dataReal", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                            
                            if (dataRealField != null)
                            {
                                var value = dataRealField.GetValue(comp);
                                if (value is int intValue)
                                {
                                    // dataReal is 0-100 (already percentage)
                                    return intValue;
                                }
                            }
                            
                            // Fallback: try dataShown field
                            var dataShownField = compType.GetField("dataShown",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            
                            if (dataShownField != null)
                            {
                                var value = dataShownField.GetValue(comp);
                                if (value is int intValue)
                                {
                                    return intValue;
                                }
                            }
                        }
                    }
                }
            }
            
            return -1;
        }

        private static string GetStatLevel(int value)
        {
            // Convert numeric value to descriptive level
            if (value <= 10) return "crítico";
            if (value <= 25) return "muito baixo";
            if (value <= 40) return "baixo";
            if (value <= 60) return "médio";
            if (value <= 75) return "alto";
            if (value <= 90) return "muito alto";
            return "máximo";
        }

        /// <summary>
        /// Gets which stats will be affected by a card swipe.
        /// Returns a string like "Afeta: Igreja ↑, Povo ↓" or empty if no changes.
        /// Reads the REAL change value from DataAnim.addReal (not the UI display).
        /// </summary>
        public static string GetStatChanges(string direction)
        {
            // Check which stats will actually change based on DataAnim.addReal field
            // addReal = actual change amount (can be 0 when addShown has a value)
            // We only report stats that have a NON-ZERO addReal value
            var affected = new List<string>();
            string[] statNames = { "spiritual", "demography", "military", "treasure" };
            string[] statLabels = { 
                Localization.Get("stat_church"), 
                Localization.Get("stat_people"), 
                Localization.Get("stat_army"), 
                Localization.Get("stat_treasury") 
            };

            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            
            for (int i = 0; i < statNames.Length; i++)
            {
                foreach (var obj in allObjects)
                {
                    if (obj.name.Equals(statNames[i], StringComparison.OrdinalIgnoreCase) && obj.activeInHierarchy)
                    {
                        // Get the DataAnim component and read addReal via reflection
                        var components = obj.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp == null) continue;
                            var compType = comp.GetType();
                            if (compType.Name == "DataAnim")
                            {
                                // Read addReal field (the ACTUAL change, not the displayed value)
                                var addRealField = compType.GetField("addReal",
                                    System.Reflection.BindingFlags.Public |
                                    System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (addRealField != null)
                                {
                                    var value = addRealField.GetValue(comp);
                                    if (value is int intValue && intValue != 0)
                                    {
                                        // Non-zero change detected!
                                        string direction_symbol = intValue < 0 ? "↓" : "↑";
                                        affected.Add($"{statLabels[i]} {direction_symbol}");
                                    }
                                }
                                break;
                            }
                        }
                        break; // Found this stat, move to next
                    }
                }
            }

            return affected.Count > 0 ? "Afeta: " + string.Join(", ", affected) : "";
        }

        private static string CleanRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Remove Unity rich text tags like <color=#e2081e>
            text = Regex.Replace(text, "<[^>]*>", "");
            return text.Trim();
        }
    }
}
