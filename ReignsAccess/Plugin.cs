using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ReignsAccess.Input;
using ReignsAccess.Accessibility;
using ReignsAccess.Navigation.Screens;
using ReignsAccess.Patches;
using ReignsAccess.Core;

namespace ReignsAccess
{
    /// <summary>
    /// Main plugin class for Reigns Accessibility mod.
    /// Provides screen reader support for blind players via Tolk/NVDA.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance { get; private set; }
        private Harmony _harmony;
        
        // Language monitoring
        private string _lastDetectedLanguage = "";
        private float _languageCheckTimer = 0f;
        private const float LANGUAGE_CHECK_INTERVAL = 2f; // Check every 2 seconds

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            // Initialize Tolk for screen reader output
            if (TolkWrapper.Initialize())
            {
                // Initialize localization system
                Localization.Initialize();
                _lastDetectedLanguage = Localization.CurrentLanguage;
                TolkWrapper.Speak(Localization.Get("mod_loaded"));
            }

            // Create keyboard navigator
            KeyboardNavigator.Create(gameObject);

            // ButtonNavigator is static and doesn't need initialization

            // Create screen reader for automatic text announcements
            ScreenReader.Create(gameObject);

            // Apply Harmony patches
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            try
            {
                // Apply general patches first
                _harmony.PatchAll();
                
                // Then apply Reigns-specific patches
                ReignsPatches.Initialize(_harmony);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error applying Harmony patches: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }

        private void OnDestroy()
        {
            ScreenReader.Destroy();
            KeyboardNavigator.Destroy();
            _harmony?.UnpatchSelf();
            TolkWrapper.Shutdown();
        }
        
        private void Update()
        {
            // Check for language changes periodically
            _languageCheckTimer += UnityEngine.Time.deltaTime;
            if (_languageCheckTimer >= LANGUAGE_CHECK_INTERVAL)
            {
                _languageCheckTimer = 0f;
                CheckLanguageChange();
            }
        }
        
        /// <summary>
        /// Check if the game language has changed and reload strings if needed.
        /// </summary>
        private void CheckLanguageChange()
        {
            try
            {
                // Try to get current language from PlayerPrefs
                if (UnityEngine.PlayerPrefs.HasKey("language"))
                {
                    string currentLang = UnityEngine.PlayerPrefs.GetString("language");
                    string normalizedLang = NormalizeLanguageCode(currentLang);
                    
                    if (normalizedLang != _lastDetectedLanguage)
                    {
                        _lastDetectedLanguage = normalizedLang;
                        Localization.RefreshLanguage();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[Plugin] CheckLanguageChange error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Normalize language code to 2-letter format.
        /// </summary>
        private string NormalizeLanguageCode(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return "en";
            
            lang = lang.ToLower().Trim();
            
            // Map common variants to standard codes
            if (lang.StartsWith("pt")) return "pt";
            if (lang.StartsWith("en")) return "en";
            if (lang.StartsWith("es")) return "es";
            if (lang.StartsWith("fr")) return "fr";
            if (lang.StartsWith("de")) return "de";
            if (lang.StartsWith("it")) return "it";
            if (lang.StartsWith("ru")) return "ru";
            if (lang.StartsWith("zh")) return "zh";
            if (lang.StartsWith("ja")) return "ja";
            if (lang.StartsWith("ko")) return "ko";
            if (lang.StartsWith("pl")) return "pl";
            if (lang.StartsWith("nl")) return "nl";
            if (lang.StartsWith("tr")) return "tr";
            
            // Return first 2 characters as fallback
            return lang.Length >= 2 ? lang.Substring(0, 2) : lang;
        }
    }

    /// <summary>
    /// Plugin metadata.
    /// </summary>
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.accessibility.reignsaccess";
        public const string PLUGIN_NAME = "Reigns Access";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

