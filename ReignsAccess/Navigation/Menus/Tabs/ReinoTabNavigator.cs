using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Core;
using static ReignsAccess.Navigation.Menus.MenuHelpers;

namespace ReignsAccess.Navigation.Menus.Tabs
{
    /// <summary>
    /// Navegador da aba Reino do menu de pausa.
    /// ResponsÃ¡vel por construir e gerenciar os itens de estatÃ­sticas do reino.
    /// </summary>
    public static class ReinoTabNavigator
    {
        /// <summary>
        /// ConstrÃ³i os itens da aba Reino.
        /// </summary>
        public static List<MenuItem> BuildItems(GameObject panel)
        {
            var items = new List<MenuItem>();
            if (panel == null) return items;
            var addedObjects = new HashSet<int>();

            // 1. Tempo jogado - kingdom_stats (pode estar inativo)
            AddKingdomStatsTime(panel, items);

            // 2. Memento Mori (mortes) - endcard_stats
            AddKingdomStatItem(panel, "endcard_stats", "dea_texts", "new_endcard", Localization.Get("memento_mori"), items, addedObjects);

            // 3. FaÃ§anhas reais (objetivos) - objective_stats
            AddKingdomStatItem(panel, "objective_stats", "dea_texts", "new_objective", Localization.Get("royal_deeds"), items, addedObjects);

            // 4. Galeria de retratos (personagens) - character_stats
            AddKingdomStatItem(panel, "character_stats", "bea_texts", "new_character", Localization.Get("portrait_gallery"), items, addedObjects);

            // 5. Cartas descobertas - card_stats (Ã© um Text, nÃ£o um botÃ£o)
            AddCardStats(panel, items);

            // 6. RECORDE - highscore_stats
            AddHighscores(panel, items);

            // The shipped Steam leaderboard button calls an empty game method.
            // It belongs to the legacy mobile social layer and is intentionally
            // omitted so keyboard users are not offered a control that cannot work.

            // 7. Botão que fecha o menu e volta ao jogo
            AddReturnToGameButton(panel, items);

            
            return items;
        }

        /// <summary>
        /// Adiciona o item de tempo jogado (kingdom_stats).
        /// </summary>
        private static void AddKingdomStatsTime(GameObject panel, List<MenuItem> items)
        {
            var kingdomStatsTransform = FindRecursive(panel.transform, "kingdom_stats");
            if (kingdomStatsTransform != null && kingdomStatsTransform.gameObject.activeInHierarchy)
            {
                var timespentText = kingdomStatsTransform.Find("timespent")?.GetComponent<Text>();
                if (timespentText != null)
                {
                    string timeText = CleanText(timespentText.text);
                    if (!string.IsNullOrEmpty(timeText))
                    {
                        items.Add(new MenuItem { Label = Localization.Get("stats_prefix") + timeText, Category = "Info" });
                        
                    }
                }
            }
        }

        /// <summary>
        /// Adiciona um item de estatÃ­stica do Reino (botÃ£o com texto de progresso).
        /// </summary>
        private static void AddKingdomStatItem(GameObject panel, string buttonName, string textChildName, string badgeName, string fallbackLabel, List<MenuItem> items, HashSet<int> added)
        {
            var btnTransform = FindRecursive(panel.transform, buttonName);
            if (btnTransform == null || !btnTransform.gameObject.activeInHierarchy) return;

            var btn = btnTransform.GetComponent<Button>();
            if (btn == null || added.Contains(btn.GetInstanceID())) return;

            // Buscar o texto de progresso (ex: "0 / 29 mortes sofridas")
            var progressText = btnTransform.Find(textChildName);
            string label = fallbackLabel;

            if (progressText != null)
            {
                var textComp = progressText.GetComponent<Text>();
                if (textComp != null)
                {
                    string text = CleanText(textComp.text);
                    if (!string.IsNullOrEmpty(text))
                    {
                        label = text;
                    }
                }
            }

            var newBadge = FindRecursive(panel.transform, badgeName);
            if (newBadge != null && newBadge.gameObject.activeInHierarchy)
            {
                label += Localization.Get("new_content_suffix");
            }

            added.Add(btn.GetInstanceID());
            items.Add(new MenuItem
            {
                Label = label,
                Category = "Button",
                ButtonRef = btn,
                GameObj = btn.gameObject
            });
            
        }

        /// <summary>
        /// Adiciona o item de cartas descobertas.
        /// </summary>
        private static void AddCardStats(GameObject panel, List<MenuItem> items)
        {
            var cardStatsText = FindTextByName(panel, "card_stats");
            if (cardStatsText != null && cardStatsText.gameObject.activeInHierarchy)
            {
                string text = CleanText(cardStatsText.text);
                if (!string.IsNullOrEmpty(text))
                {
                    items.Add(new MenuItem { Label = text, Category = "Info" });
                    
                }
            }
        }

        /// <summary>
        /// Adiciona os recordes do reino.
        /// </summary>
        private static void AddHighscores(GameObject panel, List<MenuItem> items)
        {
            var highscoreTransform = FindRecursive(panel.transform, "highscore_stats");
            if (highscoreTransform == null || !highscoreTransform.gameObject.activeInHierarchy) return;

            // Verificar se hÃ¡ algum recorde vÃ¡lido antes de adicionar o cabeÃ§alho
            bool hasRecords = false;
            for (int i = 1; i <= 4; i++)
            {
                var rank = highscoreTransform.Find(i.ToString());
                var yearsText = FindRecordText(rank, "yearsinpower");
                if (yearsText != null && yearsText.gameObject.activeInHierarchy)
                {
                    string years = CleanText(yearsText.text);
                    if (!string.IsNullOrEmpty(years) && years != ".................")
                    {
                        hasRecords = true;
                        break;
                    }
                }
            }

            // Adicionar cabeÃ§alho RECORDE
            if (hasRecords)
            {
                items.Add(new MenuItem { Label = Localization.Get("record_header"), Category = "Info" });
            }

            // Adicionar os recordes individuais (yearsinpower1, kingname1, etc.)
            for (int i = 1; i <= 4; i++)
            {
                var rank = highscoreTransform.Find(i.ToString());
                var yearsText = FindRecordText(rank, "yearsinpower");
                var kingText = FindRecordText(rank, "kingname");

                if (yearsText != null && yearsText.gameObject.activeInHierarchy)
                {
                    string years = CleanText(yearsText.text);
                    string king = kingText != null ? CleanText(kingText.text) : "";

                    if (!string.IsNullOrEmpty(years) && years != ".................")
                    {
                        string record = !string.IsNullOrEmpty(king) ? $"{i}º: {king} - {years}" : $"{i}º: {years}";
                        items.Add(new MenuItem { Label = record, Category = "Info" });
                        
                    }
                }
            }

            // Se nÃ£o hÃ¡ recordes, informar
            if (!hasRecords)
            {
                items.Add(new MenuItem { Label = Localization.Get("no_record"), Category = "Info" });
            }
        }

        private static Text FindRecordText(Transform rank, string prefix)
        {
            if (rank == null) return null;

            foreach (var text in rank.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return text;
                }
            }

            return null;
        }

        /// <summary>
        /// Adiciona o botão que fecha o menu e volta ao jogo.
        /// </summary>
        private static void AddReturnToGameButton(GameObject panel, List<MenuItem> items)
        {
            var quitBtn = FindButtonByName(panel, "quit");
            if (quitBtn != null && quitBtn.gameObject.activeInHierarchy)
            {
                items.Add(new MenuItem
                {
                    Label = Localization.Get("return_to_game"),
                    Category = "Action",
                    ActionRef = PauseMenuNavigator.CloseMenu,
                    GameObj = quitBtn.gameObject
                });
            }
        }
    }
}

