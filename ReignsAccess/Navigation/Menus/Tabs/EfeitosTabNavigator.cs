using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Core;
using static ReignsAccess.Navigation.Menus.MenuHelpers;

namespace ReignsAccess.Navigation.Menus.Tabs
{
    /// <summary>
    /// Navegador da aba Efeitos do menu de pausa.
    /// Responsável por construir e gerenciar os itens de efeitos ativos.
    /// </summary>
    public static class EfeitosTabNavigator
    {
        /// <summary>
        /// Constrói os itens da aba Efeitos.
        /// Quando não há efeitos, mostra "noeffect" e "nothing".
        /// Quando há efeitos, eles aparecem dentro de slide/Viewport/Content.
        /// </summary>
        public static List<MenuItem> BuildItems(GameObject panel)
        {
            var items = new List<MenuItem>();
            if (panel == null) return items;

            var addedObjects = new HashSet<int>();

            // Verificar se existe a mensagem "sem efeitos"
            var noeffectTransform = panel.transform.Find("noeffect");
            bool hasNoEffect = (noeffectTransform != null && noeffectTransform.gameObject.activeInHierarchy);

            if (hasNoEffect)
            {
                // Cenário 1: Sem efeitos ativos
                // Adicionar o texto principal
                var noeffectText = noeffectTransform.GetComponent<Text>();
                if (noeffectText != null)
                {
                    string cleanText = CleanText(noeffectText.text);
                    if (!string.IsNullOrEmpty(cleanText))
                    {
                        items.Add(new MenuItem 
                        { 
                            Label = cleanText, 
                            Category = "Info",
                            GameObj = noeffectText.gameObject
                        });
}
                }

                // Adicionar o texto "Nada" (filho de noeffect)
                var nothingTransform = noeffectTransform.Find("nothing");
                if (nothingTransform != null && nothingTransform.gameObject.activeInHierarchy)
                {
                    var nothingText = nothingTransform.GetComponent<Text>();
                    if (nothingText != null)
                    {
                        string cleanText = CleanText(nothingText.text);
                        if (!string.IsNullOrEmpty(cleanText))
                        {
                            items.Add(new MenuItem 
                            { 
                                Label = cleanText, 
                                Category = "Info",
                                GameObj = nothingText.gameObject
                            });
}
                    }
                }
            }
            else
            {
                // Cenário 2: Há efeitos ativos
                // Buscar no ScrollRect: slide/Viewport/Content
                var slideTransform = panel.transform.Find("slide");
                if (slideTransform != null)
                {
                    var viewportTransform = slideTransform.Find("Viewport");
                    if (viewportTransform != null)
                    {
                        var contentTransform = viewportTransform.Find("Content");
                        if (contentTransform != null && contentTransform.gameObject.activeInHierarchy)
                        {
                            // Buscar todos os efeitos dentro do Content
                            // Os efeitos geralmente são GameObjects com componentes de UI (Image, Text, Button)
                            foreach (Transform child in contentTransform)
                            {
                                if (!child.gameObject.activeInHierarchy) continue;

                                // Tentar encontrar texto descritivo do efeito
                                var textComponents = child.GetComponentsInChildren<Text>();
                                foreach (var textComp in textComponents)
                                {
                                    if (!textComp.gameObject.activeInHierarchy) continue;
                                    if (addedObjects.Contains(textComp.GetInstanceID())) continue;

                                    string cleanText = CleanText(textComp.text);
                                    if (!string.IsNullOrEmpty(cleanText) && cleanText.Length > 2)
                                    {
                                        addedObjects.Add(textComp.GetInstanceID());
                                        items.Add(new MenuItem 
                                        { 
                                            Label = cleanText, 
                                            Category = "Efeito",
                                            GameObj = child.gameObject
                                        });
break; // Pegar apenas o primeiro texto significativo por efeito
                                    }
                                }
                            }
                        }
                    }
                }

                // Se não encontrou nenhum efeito no Content
                if (items.Count == 0)
                {
                    items.Add(new MenuItem { Label = Localization.Get("no_effects_found"), Category = "Info" });
}
            }

            // Botão SAIR
            var quitBtn = FindButtonByName(panel, "quit");
            if (quitBtn != null && quitBtn.gameObject.activeInHierarchy && !addedObjects.Contains(quitBtn.GetInstanceID()))
            {
                addedObjects.Add(quitBtn.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = Localization.Get("exit_button"),
                    Category = "Button",
                    ButtonRef = quitBtn,
                    GameObj = quitBtn.gameObject
                });
}

            return items;
        }
    }
}

