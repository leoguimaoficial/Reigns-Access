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
            AddKingdomStatItem(panel, "endcard_stats", "dea_texts", Localization.Get("memento_mori"), items, addedObjects);

            // 3. FaÃ§anhas reais (objetivos) - objective_stats
            AddKingdomStatItem(panel, "objective_stats", "dea_texts", Localization.Get("royal_deeds"), items, addedObjects);

            // 4. Galeria de retratos (personagens) - character_stats
            AddKingdomStatItem(panel, "character_stats", "bea_texts", Localization.Get("portrait_gallery"), items, addedObjects);

            // 5. Cartas descobertas - card_stats (Ã© um Text, nÃ£o um botÃ£o)
            AddCardStats(panel, items);

            // 6. RECORDE - highscore_stats
            AddHighscores(panel, items);

            // 7. Leaderboard - leaderboardBut (botÃ£o)
            AddLeaderboardButton(panel, items);

            // 8. BotÃ£o SAIR
            AddQuitButton(panel, items);

            
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
        private static void AddKingdomStatItem(GameObject panel, string buttonName, string textChildName, string fallbackLabel, List<MenuItem> items, HashSet<int> added)
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
                var yearsText = FindTextByName(panel, $"yearsinpower{i}");
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
                var yearsText = FindTextByName(panel, $"yearsinpower{i}");
                var kingText = FindTextByName(panel, $"kingname{i}");

                if (yearsText != null && yearsText.gameObject.activeInHierarchy)
                {
                    string years = CleanText(yearsText.text);
                    string king = kingText != null ? CleanText(kingText.text) : "";

                    if (!string.IsNullOrEmpty(years) && years != ".................")
                    {
                        string record = !string.IsNullOrEmpty(king) ? $"{i}Âº: {king} - {years}" : $"{i}Âº: {years}";
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

        /// <summary>
        /// Adiciona o botÃ£o de leaderboard.
        /// </summary>
        private static void AddLeaderboardButton(GameObject panel, List<MenuItem> items)
        {
            var leaderboardBtn = FindButtonByName(panel, "leaderboardBut");
            if (leaderboardBtn != null && leaderboardBtn.gameObject.activeInHierarchy)
            {
                items.Add(new MenuItem
                {
                    Label = Localization.Get("leaderboard"),
                    Category = "Button",
                    ButtonRef = leaderboardBtn,
                    GameObj = leaderboardBtn.gameObject
                });
            }
        }

        /// <summary>
        /// Adiciona o botÃ£o SAIR.
        /// </summary>
        private static void AddQuitButton(GameObject panel, List<MenuItem> items)
        {
            var quitBtn = FindButtonByName(panel, "quit");
            if (quitBtn != null && quitBtn.gameObject.activeInHierarchy)
            {
                items.Add(new MenuItem
                {
                    Label = Localization.Get("exit_button"),
                    Category = "Button",
                    ButtonRef = quitBtn,
                    GameObj = quitBtn.gameObject
                });
            }
        }
    }
}

