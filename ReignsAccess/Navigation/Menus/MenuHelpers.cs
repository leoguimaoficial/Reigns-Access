using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReignsAccess.Navigation.Menus
{
    /// <summary>
    /// Classe helper com métodos compartilhados entre os navegadores de menu.
    /// </summary>
    public static class MenuHelpers
    {
        /// <summary>
        /// Classe que representa um item de menu.
        /// </summary>
        public class MenuItem
        {
            public string Label;
            public string Category;
            public Slider SliderRef;
            public Toggle ToggleRef;
            public Dropdown DropdownRef;
            public Button ButtonRef;
            public GameObject GameObj;
        }

        /// <summary>
        /// Remove tags HTML/rich text e espaços extras de um texto.
        /// </summary>
        public static string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // Remove quebras de linha
            text = text.Replace("\n", " ").Replace("\r", " ");
            
            // Remove tags HTML/rich text
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "");
            
            // Remove espaços duplos
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }
            
            return text.Trim();
        }

        /// <summary>
        /// Busca recursivamente por um Transform com o nome especificado.
        /// </summary>
        public static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                var found = FindRecursive(child, name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// Busca um componente Text pelo nome do GameObject.
        /// </summary>
        public static Text FindTextByName(GameObject panel, string name)
        {
            if (panel == null) return null;

            var allTexts = panel.GetComponentsInChildren<Text>(true);
            foreach (var t in allTexts)
            {
                if (t.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Busca um componente Button pelo nome do GameObject.
        /// </summary>
        public static Button FindButtonByName(GameObject panel, string name)
        {
            if (panel == null) return null;

            var allButtons = panel.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return b;
                }
            }
            return null;
        }

    }
}
