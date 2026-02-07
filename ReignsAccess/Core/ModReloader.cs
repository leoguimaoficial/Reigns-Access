using System;
using UnityEngine;
using ReignsAccess.Accessibility;
using BepInEx.Bootstrap;

namespace ReignsAccess.Core
{
    /// <summary>
    /// Handles hot-reloading of the mod (useful after language changes).
    /// Press F5 to reload the entire plugin.
    /// </summary>
    public static class ModReloader
    {
        /// <summary>
        /// Attempt to reload the entire plugin by calling OnDestroy and Awake again.
        /// </summary>
        public static void ReloadPlugin()
        {
            try
            {
                // Tentar encontrar a instância do Plugin
                var pluginInstance = Plugin.Instance;
                if (pluginInstance != null)
                {
                    // Chamar OnDestroy para limpar recursos
                    var onDestroyMethod = typeof(Plugin).GetMethod("OnDestroy", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    if (onDestroyMethod != null)
                    {
                        onDestroyMethod.Invoke(pluginInstance, null);
                    }
                    // Aguardar um frame
                    pluginInstance.StartCoroutine(WaitAndRestart(pluginInstance));
                }
            }
            catch (Exception) { }
        }

        private static System.Collections.IEnumerator WaitAndRestart(Plugin pluginInstance)
        {
            // Esperar alguns frames para garantir que tudo foi limpo
            yield return null;
            yield return null;
            
            // Chamar Awake para reinicializar
            var awakeMethod = typeof(Plugin).GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(pluginInstance, null);
            }
            yield return null;
        }
    }
}
