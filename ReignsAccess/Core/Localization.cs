using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ReignsAccess.Core
{
    /// <summary>
    /// Manages localization for the accessibility mod.
    /// Loads strings from JSON files based on game language.
    /// Community can add new languages by creating JSON files in the Lang folder.
    /// </summary>
    public static class Localization
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();
        private static string _currentLanguage = "en";
        private static string _langFolder;
        private static bool _initialized = false;
        private static float _lastLanguageCheck = 0f;
        private static string _lastDetectedGameLanguage = "";

        private const string EmbeddedLanguageResourcePrefix = "ReignsAccess.Lang.";
        
        /// <summary>
        /// Current language code (pt, en, es, etc.)
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Initialize the localization system.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // Get the lang folder path (next to the DLL)
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string pluginsFolder = Path.GetDirectoryName(dllPath);
                _langFolder = Path.Combine(pluginsFolder, "ReignsAccess_Lang");

                // Create lang folder if it doesn't exist
                if (!Directory.Exists(_langFolder))
                {
                    Directory.CreateDirectory(_langFolder);
                }

                // Create default language files if they don't exist
                CreateDefaultLanguageFiles();

                // Detect game language
                DetectGameLanguage();

                // Load the appropriate language
                LoadLanguage(_currentLanguage);

                _initialized = true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Localization] Initialize error: {ex.Message}");
                // Fallback to embedded strings
                LoadFallbackStrings();
                _initialized = true;
            }
        }

        /// <summary>
        /// Detect the game's current language setting.
        /// </summary>
        public static void DetectGameLanguage()
        {
            try
            {
                // Method 1: Try to find LocalizedText or similar class
                var localizedTextType = FindType("LocalizedText");
                if (localizedTextType != null)
                {
                    var languageField = localizedTextType.GetField("language", BindingFlags.Public | BindingFlags.Static);
                    if (languageField != null)
                    {
                        string lang = languageField.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(lang))
                        {
                            _currentLanguage = NormalizeLanguageCode(lang);
                            return;
                        }
                    }
                }

                // Method 2: Try to find GameManager or similar with language setting
                var gameManagerType = FindType("GameManager");
                if (gameManagerType != null)
                {
                    var instance = UnityEngine.Object.FindObjectOfType(gameManagerType as Type);
                    if (instance != null)
                    {
                        var langField = gameManagerType.GetField("language", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (langField != null)
                        {
                            var lang = langField.GetValue(instance);
                            if (lang != null)
                            {
                                _currentLanguage = NormalizeLanguageCode(lang.ToString());
                                return;
                            }
                        }
                    }
                }

                // Method 3: Look for language in PlayerPrefs
                if (PlayerPrefs.HasKey("language"))
                {
                    string lang = PlayerPrefs.GetString("language");
                    _currentLanguage = NormalizeLanguageCode(lang);
                    return;
                }

                // Method 4: Search for UI text that indicates language
                var uiTexts = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
                foreach (var text in uiTexts)
                {
                    if (text.gameObject.name.ToLower().Contains("language"))
                    {
                        string lang = DetectLanguageFromText(text.text);
                        if (!string.IsNullOrEmpty(lang))
                        {
                            _currentLanguage = lang;
                            return;
                        }
                    }
                }

                _currentLanguage = GetSystemLanguage();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Localization] DetectGameLanguage error: {ex.Message}");
                _currentLanguage = "en";
            }
        }

        /// <summary>
        /// Update - check for language changes periodically.
        /// Call this from a MonoBehaviour Update().
        /// </summary>
        public static void Update()
        {
            if (!_initialized) return;
            
            // Check every 2 seconds
            if (Time.unscaledTime - _lastLanguageCheck < 2f) return;
            _lastLanguageCheck = Time.unscaledTime;
            
            // Detect current game language
            string gameLanguage = GetCurrentGameLanguage();
            
            // If game language changed, refresh mod language
            if (!string.IsNullOrEmpty(gameLanguage) && gameLanguage != _lastDetectedGameLanguage)
            {
                _lastDetectedGameLanguage = gameLanguage;
                string normalizedLang = NormalizeLanguageCode(gameLanguage);
                
                if (normalizedLang != _currentLanguage)
                {
                    LoadLanguage(normalizedLang);
                }
            }
        }
        
        /// <summary>
        /// Get current game language from PlayerPrefs (quick check).
        /// </summary>
        private static string GetCurrentGameLanguage()
        {
            try
            {
                if (PlayerPrefs.HasKey("language"))
                {
                    return PlayerPrefs.GetString("language");
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Refresh language detection (call when game language might have changed).
        /// </summary>
        public static void RefreshLanguage()
        {
            string oldLanguage = _currentLanguage;
            DetectGameLanguage();
            
            if (oldLanguage != _currentLanguage)
            {
                LoadLanguage(_currentLanguage);
            }
        }
        
        /// <summary>
        /// Force reload current language (useful when language files are updated).
        /// </summary>
        public static void ForceReload()
        {
            LoadLanguage(_currentLanguage);
        }

        /// <summary>
        /// Get a localized string by key.
        /// </summary>
        public static string Get(string key)
        {
            if (!_initialized)
                Initialize();

            if (_strings.TryGetValue(key, out string value))
                return value;

            return key;
        }

        /// <summary>
        /// Get a localized string with format arguments.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Load a specific language.
        /// </summary>
        public static void LoadLanguage(string langCode)
        {
            langCode = NormalizeLanguageCode(langCode);
            string filePath = Path.Combine(_langFolder, $"{langCode}.json");

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_langFolder, "en.json");
            }

            if (!File.Exists(filePath))
            {
                LoadFallbackStrings();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                _strings = ParseJson(json);
                _currentLanguage = langCode;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Localization] Error loading language file: {ex.Message}");
                LoadFallbackStrings();
            }
        }

        /// <summary>
        /// Set language manually.
        /// </summary>
        public static void SetLanguage(string langCode)
        {
            LoadLanguage(langCode);
        }

        /// <summary>
        /// Get list of available languages (both bundled and community).
        /// </summary>
        public static string[] GetAvailableLanguages()
        {
            var languages = new List<string>();
            
            if (Directory.Exists(_langFolder))
            {
                foreach (var file in Directory.GetFiles(_langFolder, "*.json"))
                {
                    string code = Path.GetFileNameWithoutExtension(file);
                    if (!code.StartsWith("_") && code != "template") // Skip template and metadata files
                    {
                        languages.Add(code);
                    }
                }
            }
            
            return languages.ToArray();
        }

        // === Private Methods ===

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeName)
                            return type;
                    }
                }
                catch { }
            }
            return null;
        }

        private static string NormalizeLanguageCode(string lang)
        {
            if (string.IsNullOrEmpty(lang))
                return "en";

            lang = lang.ToLower().Trim();
            
            // Map game language codes to mod language codes
            // Reigns uses: bp (Portuguese), en (English), es (Spanish), fr (French), etc.
            switch (lang)
            {
                case "bp": return "pt";  // Brazilian Portuguese -> Portuguese
                case "pt": return "pt";  // Portuguese
                case "en": return "en";  // English
                case "es": return "es";  // Spanish
                case "fr": return "fr";  // French
                case "de": return "de";  // German
                case "it": return "it";  // Italian
                case "ru": return "ru";  // Russian
                case "zh": return "zh";  // Chinese
                case "ja": return "ja";  // Japanese
                case "ko": return "ko";  // Korean
                case "pl": return "pl";  // Polish
                case "nl": return "nl";  // Dutch
                case "tr": return "tr";  // Turkish
                default:
                    // Fallback: use first 2 chars
                    if (lang.Length >= 2)
                        return lang.Substring(0, 2);
                    return lang;
            }
        }

        private static string GetSystemLanguage()
        {
            try
            {
                var sysLang = Application.systemLanguage;
                switch (sysLang)
                {
                    case SystemLanguage.Portuguese: return "pt";
                    case SystemLanguage.English: return "en";
                    case SystemLanguage.Spanish: return "es";
                    case SystemLanguage.French: return "fr";
                    case SystemLanguage.German: return "de";
                    case SystemLanguage.Italian: return "it";
                    case SystemLanguage.Russian: return "ru";
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                    case SystemLanguage.ChineseTraditional: return "zh";
                    case SystemLanguage.Japanese: return "ja";
                    case SystemLanguage.Korean: return "ko";
                    case SystemLanguage.Polish: return "pl";
                    case SystemLanguage.Dutch: return "nl";
                    case SystemLanguage.Turkish: return "tr";
                    default: return "en";
                }
            }
            catch
            {
                return "en";
            }
        }

        private static string DetectLanguageFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            
            text = text.ToLower();
            
            if (text.Contains("português") || text.Contains("portuguese") || text.Contains("brazil"))
                return "pt";
            if (text.Contains("english") || text.Contains("inglês"))
                return "en";
            if (text.Contains("español") || text.Contains("spanish"))
                return "es";
            if (text.Contains("français") || text.Contains("french"))
                return "fr";
            if (text.Contains("deutsch") || text.Contains("german"))
                return "de";
            if (text.Contains("italiano") || text.Contains("italian"))
                return "it";
            if (text.Contains("русский") || text.Contains("russian"))
                return "ru";
            if (text.Contains("polski") || text.Contains("polish"))
                return "pl";
            if (text.Contains("nederlands") || text.Contains("dutch"))
                return "nl";
            if (text.Contains("türkçe") || text.Contains("turkish"))
                return "tr";
            
            return null;
        }

        private static Dictionary<string, string> ParseJson(string json)
        {
            var result = new Dictionary<string, string>();
            
            // Simple JSON parser for flat key-value pairs
            // Format: { "key": "value", "key2": "value2" }
            
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            int i = 0;
            while (i < json.Length)
            {
                // Skip whitespace
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length) break;

                // Expect key starting with quote
                if (json[i] != '"')
                {
                    i++;
                    continue;
                }

                // Parse key
                string key = ParseJsonString(json, ref i);
                if (key == null) break;

                // Skip to colon
                while (i < json.Length && json[i] != ':') i++;
                if (i >= json.Length) break;
                i++; // skip colon

                // Skip whitespace
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length) break;

                // Parse value
                if (json[i] != '"')
                {
                    // Skip non-string values
                    while (i < json.Length && json[i] != ',' && json[i] != '}') i++;
                    continue;
                }

                string value = ParseJsonString(json, ref i);
                if (value != null)
                {
                    result[key] = value;
                }

                // Skip to next pair
                while (i < json.Length && json[i] != ',' && json[i] != '}') i++;
                if (i < json.Length && json[i] == ',') i++;
            }

            return result;
        }

        private static string ParseJsonString(string json, ref int i)
        {
            if (i >= json.Length || json[i] != '"')
                return null;

            i++; // skip opening quote
            var sb = new System.Text.StringBuilder();

            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    i++; // skip closing quote
                    return sb.ToString();
                }
                if (c == '\\' && i + 1 < json.Length)
                {
                    i++;
                    char escaped = json[i];
                    switch (escaped)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append(escaped); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
                i++;
            }

            return sb.ToString();
        }

        private static void CreateDefaultLanguageFiles()
        {
            // Prefer language JSONs bundled as embedded resources.
            // This lets "DLL-only" installs still recreate all languages automatically.
            bool extractedFromResources = CreateLanguageFilesFromEmbeddedResources();

            if (!extractedFromResources)
            {
                // Fallback for older builds without embedded resources.
                CreateLanguageFile("pt", GetPortugueseJson());
                CreateLanguageFile("en", GetEnglishJson());
            }
        }

        private static bool CreateLanguageFilesFromEmbeddedResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();
                bool extractedAny = false;

                foreach (string resourceName in resourceNames)
                {
                    if (!resourceName.StartsWith(EmbeddedLanguageResourcePrefix, StringComparison.OrdinalIgnoreCase) ||
                        !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string fileName = resourceName.Substring(EmbeddedLanguageResourcePrefix.Length);
                    if (fileName.Equals("template.json", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("_template.json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string filePath = Path.Combine(_langFolder, fileName);
                    if (File.Exists(filePath))
                    {
                        continue;
                    }

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            continue;
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            if (string.IsNullOrWhiteSpace(json))
                            {
                                continue;
                            }

                            File.WriteAllText(filePath, json);
                            extractedAny = true;
                        }
                    }
                }

                return extractedAny;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Localization] Error extracting embedded language files: {ex.Message}");
                return false;
            }
        }

        private static void CreateLanguageFile(string langCode, string json)
        {
            string filePath = Path.Combine(_langFolder, $"{langCode}.json");
            if (File.Exists(filePath)) return;

            try
            {
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Localization] Error creating {langCode} file: {ex.Message}");
            }
        }

        private static void LoadFallbackStrings()
        {
            // Load embedded Portuguese strings as fallback
            string json = GetPortugueseJson();
            _strings = ParseJson(json);
            _currentLanguage = "pt";
        }

        // === Embedded Language Files (bundled defaults) ===
        // Community can add more languages by creating JSON files in BepInEx/plugins/ReignsAccess_Lang/

        private static string GetPortugueseJson()
        {
            return @"{
  ""_language_name"": ""Português"",
  ""_language_code"": ""pt"",
  ""_author"": ""Reigns Access Team"",
  
  ""mod_loaded"": ""Reigns Access carregado. Use as setas para navegar, Enter para selecionar."",
  ""game_loaded"": ""Jogo carregado. Pressione H para ajuda."",
  ""screen_prefix"": ""Tela: "",
  
  ""character_says"": "" diz: "",
  ""options_prefix"": "". Opções: "",
  ""or"": "" ou "",
  ""no_narrative"": ""Nenhum texto narrativo para repetir"",
  
  ""advancing"": ""Avançando"",
  ""position_of"": "" de "",
  
  ""death_screen"": ""Morte do Rei"",
  ""years_prefix"": ""Anos: "",
  ""king_prefix"": ""Rei: "",
  
  ""chrono_screen"": ""Cronologia"",
  ""year_prefix"": ""Ano "",
  ""objective_prefix"": ""Objetivo: "",
  
  ""dialog_default"": ""Diálogo"",
  ""selected_prefix"": ""Selecionado: "",
  ""buttons_prefix"": "". Botões: "",
  ""dialog_nav_hint"": "". Use setas esquerda/direita para navegar e Enter para selecionar"",
  ""nav_hint"": "". Setas para navegar, Enter para ativar."",
  ""activated"": "" ativado"",
  ""button_fallback"": ""Botão"",
  
  ""tab_reino"": ""Reino"",
  ""tab_efeitos"": ""Efeitos"",
  ""tab_opcoes"": ""Opções"",
  ""tab_unknown"": ""Desconhecida"",
  ""pause_menu_opened"": ""Menu de pausa. Aba "",
  ""items_suffix"": "" itens."",
  ""tab_prefix"": ""Aba "",
  ""tab_nav_hint"": "" Use setas para navegar."",
  ""menu_closed"": ""Menu fechado"",
  ""tab_switch_error"": ""Erro ao trocar aba"",
  ""tab_not_available"": ""Botão de aba não disponível"",
  ""tab_not_found"": ""Botão de aba não encontrado"",
  ""toggle_on"": "": Ativado"",
  ""toggle_off"": "": Desativado"",
  ""not_available"": "" não está disponível"",
  ""activation_error"": ""Erro ao ativar "",
  ""slider_hint"": ""Use setas esquerda/direita. Atual: "",
  ""info_only"": "". Apenas informação, não clicável."",
  ""no_action"": "". Sem ação disponível."",
  ""no_items"": ""Nenhum item nesta aba"",
  
  ""memento_mori"": ""Memento mori"",
  ""royal_deeds"": ""Façanhas reais"",
  ""portrait_gallery"": ""Galeria de retratos"",
  ""stats_prefix"": ""Estatísticas: "",
  ""record_header"": ""RECORDE"",
  ""no_record"": ""RECORDE: Nenhum recorde ainda"",
  ""leaderboard"": ""Placar de líderes"",
  ""exit_button"": ""SAIR"",
  
  ""opt_sfx_volume"": ""Volume de sons"",
  ""opt_music_volume"": ""Volume de música"",
  ""opt_voice_over"": ""Personagens dublados"",
  ""opt_fullscreen"": ""Tela cheia"",
  ""opt_language"": ""Idioma"",
  ""opt_apply_language"": ""Aplicar idioma"",
  ""opt_resolution"": ""Resolução de tela"",
  ""opt_apply_resolution"": ""Aplicar resolução"",
  ""opt_more_reigns"": ""Mais Reigns! Mais Amor!"",
  
  ""no_effects_found"": ""Nenhum efeito encontrado"",
  
  ""no_card"": ""Nenhuma carta visível"",
  ""right_prefix"": ""Direita: "",
  ""left_prefix"": "". Esquerda: "",
  ""yes_default"": ""Sim"",
  ""no_default"": ""Não"",
  ""no_stats_affected"": ""Nenhuma estatística será afetada"",
  ""stat_not_available"": "" não disponível"",
  ""stats_not_available"": ""Estatísticas não disponíveis"",
  ""stat_church"": ""Igreja"",
  ""stat_people"": ""Povo"",
  ""stat_army"": ""Exército"",
  ""stat_treasury"": ""Tesouro"",
  ""king_info_prefix"": ""Rei "",
  ""year_info_prefix"": "". Ano "",
  ""years_in_power"": "" anos no poder"",
  ""king_info_error"": ""Informações do rei não disponíveis"",
  ""no_objective"": ""Nenhum objetivo ativo"",
  
  ""help_intro"": ""Teclas de acessibilidade. "",
  ""help_arrows"": ""Seta cima lê carta. Seta baixo todas estatísticas. "",
  ""help_swipe"": ""Setas esquerda e direita inclinam a carta e leem a opção. Pressione novamente para confirmar. "",
  ""help_options"": ""E lê as duas opções. T mostra quais estatísticas serão afetadas. "",
  ""help_stats"": ""A igreja, S povo, D exército, F tesouro. "",
  ""help_info"": ""I informações do rei. O objetivo. "",
  ""help_menu"": ""ESC abre menu de pausa. No menu: setas navegam, Enter seleciona, ESC volta. "",
  ""help_general"": ""Q silencia. H esta ajuda."",
  
  ""swipe_options_unavailable"": ""Opções de deslize não disponíveis"",
  ""no_for"": ""N para Não: "",
  ""yes_for"": ""Y para Sim: "",
  
  ""select_option_hint"": ""Selecione uma opção com seta esquerda ou direita."",
  ""chosen_prefix"": ""Escolhido: "",
  ""action_failed"": ""Não foi possível executar a ação."",
  
  ""use_arrows_hint"": ""Use setas para navegar e Enter para selecionar"",
  ""options_dialog"": ""Opções: "",
  
  ""unknown_death"": ""Morte desconhecida"",
  ""unknown_objective"": ""Objetivo desconhecido"",
  ""unknown_character"": ""Personagem desconhecido"",
  ""nickname_prefix"": ""Apelido: "",
  ""completed"": ""Completado"",
  ""not_completed"": ""Não completado""
}";
        }

        private static string GetEnglishJson()
        {
            return @"{
  ""_language_name"": ""English"",
  ""_language_code"": ""en"",
  ""_author"": ""Reigns Access Team"",
  
  ""mod_loaded"": ""Reigns Access loaded. Use arrow keys to navigate, Enter to select."",
  ""game_loaded"": ""Game loaded. Press H for help."",
  ""screen_prefix"": ""Screen: "",
  
  ""character_says"": "" says: "",
  ""options_prefix"": "". Options: "",
  ""or"": "" or "",
  ""no_narrative"": ""No narrative text to repeat"",
  
  ""advancing"": ""Advancing"",
  ""position_of"": "" of "",
  
  ""death_screen"": ""King's Death"",
  ""years_prefix"": ""Years: "",
  ""king_prefix"": ""King: "",
  
  ""chrono_screen"": ""Chronology"",
  ""year_prefix"": ""Year "",
  ""objective_prefix"": ""Objective: "",
  
  ""dialog_default"": ""Dialog"",
  ""selected_prefix"": ""Selected: "",
  ""buttons_prefix"": "". Buttons: "",
  ""dialog_nav_hint"": "". Use left/right arrows to navigate and Enter to select"",
  ""nav_hint"": "". Arrows to navigate, Enter to activate."",
  ""activated"": "" activated"",
  ""button_fallback"": ""Button"",
  
  ""tab_reino"": ""Kingdom"",
  ""tab_efeitos"": ""Effects"",
  ""tab_opcoes"": ""Options"",
  ""tab_unknown"": ""Unknown"",
  ""pause_menu_opened"": ""Pause menu. Tab "",
  ""items_suffix"": "" items."",
  ""tab_prefix"": ""Tab "",
  ""tab_nav_hint"": "" Use arrows to navigate."",
  ""menu_closed"": ""Menu closed"",
  ""tab_switch_error"": ""Error switching tab"",
  ""tab_not_available"": ""Tab button not available"",
  ""tab_not_found"": ""Tab button not found"",
  ""toggle_on"": "": On"",
  ""toggle_off"": "": Off"",
  ""not_available"": "" is not available"",
  ""activation_error"": ""Error activating "",
  ""slider_hint"": ""Use left/right arrows. Current: "",
  ""info_only"": "". Information only, not clickable."",
  ""no_action"": "". No action available."",
  ""no_items"": ""No items in this tab"",
  
  ""memento_mori"": ""Memento mori"",
  ""royal_deeds"": ""Royal deeds"",
  ""portrait_gallery"": ""Portrait gallery"",
  ""stats_prefix"": ""Statistics: "",
  ""record_header"": ""RECORD"",
  ""no_record"": ""RECORD: No record yet"",
  ""leaderboard"": ""Leaderboard"",
  ""exit_button"": ""EXIT"",
  
  ""opt_sfx_volume"": ""Sound volume"",
  ""opt_music_volume"": ""Music volume"",
  ""opt_voice_over"": ""Voiced characters"",
  ""opt_fullscreen"": ""Fullscreen"",
  ""opt_language"": ""Language"",
  ""opt_apply_language"": ""Apply language"",
  ""opt_resolution"": ""Screen resolution"",
  ""opt_apply_resolution"": ""Apply resolution"",
  ""opt_more_reigns"": ""More Reigns! More Love!"",
  
  ""no_effects_found"": ""No effects found"",
  
  ""no_card"": ""No card visible"",
  ""right_prefix"": ""Right: "",
  ""left_prefix"": "". Left: "",
  ""yes_default"": ""Yes"",
  ""no_default"": ""No"",
  ""no_stats_affected"": ""No stats will be affected"",
  ""stat_not_available"": "" not available"",
  ""stats_not_available"": ""Stats not available"",
  ""stat_church"": ""Church"",
  ""stat_people"": ""People"",
  ""stat_army"": ""Army"",
  ""stat_treasury"": ""Treasury"",
  ""king_info_prefix"": ""King "",
  ""year_info_prefix"": "". Year "",
  ""years_in_power"": "" years in power"",
  ""king_info_error"": ""King information not available"",
  ""no_objective"": ""No active objective"",
  
  ""help_intro"": ""Accessibility keys. "",
  ""help_arrows"": ""Up arrow reads card. Down arrow all stats. "",
  ""help_swipe"": ""Left and right arrows tilt card and read option. Press again to confirm. "",
  ""help_options"": ""E reads both options. T shows which stats will be affected. "",
  ""help_stats"": ""A church, S people, D army, F treasury. "",
  ""help_info"": ""I king information. O objective. "",
  ""help_menu"": ""ESC opens pause menu. In menu: arrows navigate, Enter selects, ESC goes back. "",
  ""help_general"": ""Q mutes. H this help."",
  
  ""swipe_options_unavailable"": ""Swipe options not available"",
  ""no_for"": ""N for No: "",
  ""yes_for"": ""Y for Yes: "",
  
  ""select_option_hint"": ""Select an option with left or right arrow."",
  ""chosen_prefix"": ""Chosen: "",
  ""action_failed"": ""Could not execute action."",
  
  ""use_arrows_hint"": ""Use arrows to navigate and Enter to select"",
  ""options_dialog"": ""Options: "",
  
  ""unknown_death"": ""Unknown death"",
  ""unknown_objective"": ""Unknown objective"",
  ""unknown_character"": ""Unknown character"",
  ""nickname_prefix"": ""Nickname: "",
  ""completed"": ""Completed"",
  ""not_completed"": ""Not completed""
}";
        }

        private static string GetTemplateJson()
        {
            return @"{
  ""_language_name"": ""YOUR LANGUAGE NAME (e.g., Español, Français, Deutsch)"",
  ""_language_code"": ""LANGUAGE_CODE (e.g., es, fr, de)"",
  ""_author"": ""YOUR NAME / YOUR COMMUNITY"",
  ""_notes"": ""Translate the English values below to your language. Save as [language_code].json (e.g., es.json for Spanish). Language code must match Reigns game setting."",
  
  ""mod_loaded"": ""Reigns Access loaded. Use arrow keys to navigate, Enter to select."",
  ""game_loaded"": ""Game loaded. Press H for help."",
  ""screen_prefix"": ""Screen: "",
  
  ""character_says"": "" says: "",
  ""options_prefix"": "". Options: "",
  ""or"": "" or "",
  ""no_narrative"": ""No narrative text to repeat"",
  
  ""advancing"": ""Advancing"",
  ""position_of"": "" of "",
  
  ""death_screen"": ""King's Death"",
  ""years_prefix"": ""Years: "",
  ""king_prefix"": ""King: "",
  
  ""chrono_screen"": ""Chronology"",
  ""year_prefix"": ""Year "",
  ""objective_prefix"": ""Objective: "",
  
  ""dialog_default"": ""Dialog"",
  ""selected_prefix"": ""Selected: "",
  ""buttons_prefix"": "". Buttons: "",
  ""dialog_nav_hint"": "". Use left/right arrows to navigate and Enter to select"",
  ""nav_hint"": "". Arrows to navigate, Enter to activate."",
  ""activated"": "" activated"",
  ""button_fallback"": ""Button"",
  
  ""tab_reino"": ""Kingdom"",
  ""tab_efeitos"": ""Effects"",
  ""tab_opcoes"": ""Options"",
  ""tab_unknown"": ""Unknown"",
  ""pause_menu_opened"": ""Pause menu. Tab "",
  ""items_suffix"": "" items."",
  ""tab_prefix"": ""Tab "",
  ""tab_nav_hint"": "" Use arrows to navigate."",
  ""menu_closed"": ""Menu closed"",
  ""tab_switch_error"": ""Error switching tab"",
  ""tab_not_available"": ""Tab button not available"",
  ""tab_not_found"": ""Tab button not found"",
  ""toggle_on"": "": On"",
  ""toggle_off"": "": Off"",
  ""not_available"": "" is not available"",
  ""activation_error"": ""Error activating "",
  ""slider_hint"": ""Use left/right arrows. Current: "",
  ""info_only"": "". Information only, not clickable."",
  ""no_action"": "". No action available."",
  ""no_items"": ""No items in this tab"",
  
  ""memento_mori"": ""Memento mori"",
  ""royal_deeds"": ""Royal deeds"",
  ""portrait_gallery"": ""Portrait gallery"",
  ""stats_prefix"": ""Statistics: "",
  ""record_header"": ""RECORD"",
  ""no_record"": ""RECORD: No record yet"",
  ""leaderboard"": ""Leaderboard"",
  ""exit_button"": ""EXIT"",
  
  ""opt_sfx_volume"": ""Sound volume"",
  ""opt_music_volume"": ""Music volume"",
  ""opt_voice_over"": ""Voiced characters"",
  ""opt_fullscreen"": ""Fullscreen"",
  ""opt_language"": ""Language"",
  ""opt_apply_language"": ""Apply language"",
  ""opt_resolution"": ""Screen resolution"",
  ""opt_apply_resolution"": ""Apply resolution"",
  ""opt_more_reigns"": ""More Reigns! More Love!"",
  
  ""no_effects_found"": ""No effects found"",
  
  ""no_card"": ""No card visible"",
  ""right_prefix"": ""Right: "",
  ""left_prefix"": "". Left: "",
  ""yes_default"": ""Yes"",
  ""no_default"": ""No"",
  ""no_stats_affected"": ""No stats will be affected"",
  ""stat_not_available"": "" not available"",
  ""stats_not_available"": ""Stats not available"",
  ""stat_church"": ""Church"",
  ""stat_people"": ""People"",
  ""stat_army"": ""Army"",
  ""stat_treasury"": ""Treasury"",
  ""king_info_prefix"": ""King "",
  ""year_info_prefix"": "". Year "",
  ""years_in_power"": "" years in power"",
  ""king_info_error"": ""King information not available"",
  ""no_objective"": ""No active objective"",
  
  ""help_intro"": ""Accessibility keys. "",
  ""help_arrows"": ""Up arrow reads card. Down arrow all stats. "",
  ""help_swipe"": ""Left and right arrows tilt card and read option. Press again to confirm. "",
  ""help_options"": ""E reads both options. T shows which stats will be affected. "",
  ""help_stats"": ""A church, S people, D army, F treasury. "",
  ""help_info"": ""I king information. O objective. "",
  ""help_menu"": ""ESC opens pause menu. In menu: arrows navigate, Enter selects, ESC goes back. "",
  ""help_general"": ""Q mutes. H this help."",
  
  ""swipe_options_unavailable"": ""Swipe options not available"",
  ""no_for"": ""N for No: "",
  ""yes_for"": ""Y for Yes: "",
  
  ""select_option_hint"": ""Select an option with left or right arrow."",
  ""chosen_prefix"": ""Chosen: "",
  ""action_failed"": ""Could not execute action."",
  
  ""use_arrows_hint"": ""Use arrows to navigate and Enter to select"",
  ""options_dialog"": ""Options: "",
  
  ""unknown_death"": ""Unknown death"",
  ""unknown_objective"": ""Unknown objective"",
  ""unknown_character"": ""Unknown character"",
  ""nickname_prefix"": ""Nickname: "",
  ""completed"": ""Completed"",
  ""not_completed"": ""Not completed""
}";
        }
    }
}
