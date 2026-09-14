using System;
using System.Collections.Generic;
using System.Globalization;
using ReignsAccess.Core;
using UnityEngine;
using static ReignsAccess.Navigation.Menus.MenuHelpers;

namespace ReignsAccess.Navigation.Menus.Tabs
{
    /// <summary>
    /// Builds the Effects tab from the game's live effect model.
    /// </summary>
    public static class EfeitosTabNavigator
    {
        public static List<MenuItem> BuildItems(GameObject panel)
        {
            var items = new List<MenuItem>();
            if (panel == null) return items;

            // EffectsStats creates and destroys visual EffectBox prefabs whenever
            // the panel changes state. Reading the first child Text captured stale
            // placeholders and omitted descriptions. Its EffectAct reference is
            // the authoritative source used by the game itself.
            EffectsStats stats = panel.GetComponent<EffectsStats>();
            EffectAct effectController = stats != null ? stats.scEf : null;
            var liveEffects = effectController != null ? effectController.GetLiveEffects() : null;

            if (liveEffects != null && liveEffects.Count > 0)
            {
                foreach (var effect in liveEffects)
                {
                    if (effect == null) continue;
                    items.Add(new MenuItem
                    {
                        Label = BuildEffectLabel(effect),
                        Category = "Info"
                    });
                }
            }
            else
            {
                // The serialized prefab contains English placeholder strings.
                // Use the mod locale when there is no live effect.
                items.Add(new MenuItem
                {
                    Label = Localization.Get("no_effects_found"),
                    Category = "Info"
                });
            }

            var quitButton = FindButtonByName(panel, "quit");
            if (quitButton != null && quitButton.gameObject.activeInHierarchy)
            {
                items.Add(new MenuItem
                {
                    Label = Localization.Get("return_to_game"),
                    Category = "Action",
                    ActionRef = PauseMenuNavigator.CloseMenu,
                    GameObj = quitButton.gameObject
                });
            }

            return items;
        }

        private static string BuildEffectLabel(Effect effect)
        {
            var parts = new List<string>();

            try
            {
                string title = CleanText(effect.title);
                string description = CleanText(effect.description);
                if (!string.IsNullOrEmpty(title)) parts.Add(title);
                if (!string.IsNullOrEmpty(description) &&
                    !description.Equals(title, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add(description);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[Effects] Could not localize effect {effect.tag}: {ex.Message}");
            }

            var outcomes = new List<string>();
            if (effect.outcomes != null)
            {
                foreach (var outcome in effect.outcomes)
                {
                    string stat = GetStatLabel(outcome.variable);
                    if (string.IsNullOrEmpty(stat)) continue;

                    string value = outcome.value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
                    outcomes.Add(stat + " " + value);
                }
            }

            if (outcomes.Count > 0)
            {
                parts.Add(Localization.Get("effect_outcomes_prefix") + string.Join(", ", outcomes));
            }

            return parts.Count > 0 ? string.Join(". ", parts) : effect.tag;
        }

        private static string GetStatLabel(Variables variable)
        {
            switch (variable)
            {
                case Variables.spiritual:
                    return Localization.Get("stat_church");
                case Variables.demography:
                    return Localization.Get("stat_people");
                case Variables.military:
                    return Localization.Get("stat_army");
                case Variables.treasure:
                    return Localization.Get("stat_treasury");
                default:
                    return "";
            }
        }
    }
}
