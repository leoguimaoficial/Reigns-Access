using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ReignsAccess.Accessibility
{
    /// <summary>
    /// P/Invoke wrapper for Tolk.dll screen reader library.
    /// </summary>
    internal static class TolkNative
    {
        private const string DllName = "Tolk";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Tolk_Load();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Tolk_Unload();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Tolk_IsLoaded();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string str, bool interrupt);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern bool Tolk_Speak([MarshalAs(UnmanagedType.LPWStr)] string str, bool interrupt);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Tolk_Silence();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr Tolk_DetectScreenReader();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Tolk_HasSpeech();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Tolk_HasBraille();
    }

    /// <summary>
    /// Wrapper for Tolk screen reader library.
    /// Handles initialization and provides speech output with deduplication.
    /// </summary>
    public static class TolkWrapper
    {
        private static bool _isInitialized = false;
        private static bool _initFailed = false;

        // Deduplication to prevent spam
        private static string _lastSpokenText = "";
        private static float _lastSpokenTime = 0f;
        private const float DEDUP_WINDOW = 0.3f;

        public static bool IsAvailable => _isInitialized && !_initFailed;

        /// <summary>
        /// Initialize Tolk library for screen reader communication.
        /// </summary>
        public static bool Initialize()
        {
            if (_isInitialized) return true;
            if (_initFailed) return false;

            try
            {
                TolkNative.Tolk_Load();
                
                if (!TolkNative.Tolk_IsLoaded())
                {
                    _initFailed = true;
                    return false;
                }

                _isInitialized = true;
                
                IntPtr screenReaderPtr = TolkNative.Tolk_DetectScreenReader();
                
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to initialize Tolk: {ex.Message}");
                _initFailed = true;
                return false;
            }
        }

        /// <summary>
        /// Speak text through the screen reader.
        /// </summary>
        /// <param name="text">Text to speak</param>
        /// <param name="interrupt">If true, interrupts current speech</param>
        public static void Speak(string text, bool interrupt = true)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(text)) return;

            // Deduplication check
            float currentTime = Time.realtimeSinceStartup;
            if (text == _lastSpokenText && (currentTime - _lastSpokenTime) < DEDUP_WINDOW)
            {
                return;
            }

            _lastSpokenText = text;
            _lastSpokenTime = currentTime;

            try
            {
                TolkNative.Tolk_Output(text, interrupt);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Tolk.Speak error: {ex.Message}");
            }
        }

        /// <summary>
        /// Speak text, bypassing deduplication.
        /// </summary>
        public static void SpeakForced(string text, bool interrupt = true)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(text)) return;

            _lastSpokenText = text;
            _lastSpokenTime = Time.realtimeSinceStartup;

            try
            {
                TolkNative.Tolk_Output(text, interrupt);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Tolk.SpeakForced error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop current speech.
        /// </summary>
        public static void Silence()
        {
            if (!_isInitialized) return;

            try
            {
                TolkNative.Tolk_Silence();
            }
            catch
            {
                // Ignore errors when silencing
            }
        }

        /// <summary>
        /// Shutdown Tolk library.
        /// </summary>
        public static void Shutdown()
        {
            if (!_isInitialized) return;

            try
            {
                TolkNative.Tolk_Unload();
            }
            catch
            {
                // Ignore errors when unloading
            }

            _isInitialized = false;
        }
    }
}
