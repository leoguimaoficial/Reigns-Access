using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Core;
using static ReignsAccess.Navigation.Menus.MenuHelpers;

namespace ReignsAccess.Navigation.Menus.Tabs
{
    /// <summary>
    /// Navigator for pause menu options tab.
    /// </summary>
    public static class OpcoesTabNavigator
    {
        /// <summary>
        /// Builds all items from options tab.
        /// </summary>
        public static List<MenuItem> BuildItems(GameObject panel)
        {
            var items = new List<MenuItem>();
            if (panel == null) return items;

            var addedObjects = new HashSet<int>();

            // 1. Sliders
            AddSliderFromParent(panel, "sfx_volume", "opt_slider", Localization.Get("opt_sfx_volume"), items, addedObjects);
            AddSliderFromParent(panel, "music_volume", "opt_slider", Localization.Get("opt_music_volume"), items, addedObjects);

            // 2. Toggles
            AddToggleItem(panel, "togglevo", Localization.Get("opt_voice_over"), items, addedObjects);
            AddToggleItem(panel, "togglewin", Localization.Get("opt_fullscreen"), items, addedObjects);

            // 3. Dropdowns and apply buttons
            AddDropdownFromParent(panel, "language", "drop", Localization.Get("opt_language"), items, addedObjects);
            AddButtonFromParentAllowInactive(panel, "language", "applyBut", Localization.Get("opt_apply_language"), items, addedObjects);

            AddDropdownNestedParent(panel, "allresol", "resolution", "drop", Localization.Get("opt_resolution"), items, addedObjects);
            AddButtonNestedParentAllowInactive(panel, "allresol", "resolution", "applyBut", Localization.Get("opt_apply_resolution"), items, addedObjects);

            // 4. Social link button
            AddButtonItem(panel, "link1", Localization.Get("opt_more_reigns"), items, addedObjects);

            // 5. Main exit button
            AddQuitButton(panel, items, addedObjects);

            return items;
        }

        private static void AddSliderFromParent(GameObject panel, string parentName, string sliderName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var parent = panel.transform.Find(parentName);
            if (parent == null) return;

            var sliderTransform = parent.Find(sliderName);
            if (sliderTransform == null) return;

            var slider = sliderTransform.GetComponent<Slider>();
            if (slider != null && slider.interactable && !added.Contains(slider.GetInstanceID()))
            {
                added.Add(slider.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Slider",
                    SliderRef = slider,
                    GameObj = slider.gameObject
                });
            }
        }

        private static void AddDropdownFromParent(GameObject panel, string parentName, string dropdownName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var parent = panel.transform.Find(parentName);
            if (parent == null) return;

            var dropdownTransform = parent.Find(dropdownName);
            if (dropdownTransform == null) return;

            var dropdown = dropdownTransform.GetComponent<Dropdown>();
            if (dropdown != null && dropdown.interactable && !added.Contains(dropdown.GetInstanceID()))
            {
                added.Add(dropdown.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Dropdown",
                    DropdownRef = dropdown,
                    GameObj = dropdown.gameObject
                });
            }
        }

        private static void AddDropdownNestedParent(GameObject panel, string grandParentName, string parentName, string dropdownName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var grandParent = panel.transform.Find(grandParentName);
            if (grandParent == null) return;

            var parent = grandParent.Find(parentName);
            if (parent == null) return;

            var dropdownTransform = parent.Find(dropdownName);
            if (dropdownTransform == null) return;

            var dropdown = dropdownTransform.GetComponent<Dropdown>();
            if (dropdown != null && dropdown.interactable && !added.Contains(dropdown.GetInstanceID()))
            {
                added.Add(dropdown.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Dropdown",
                    DropdownRef = dropdown,
                    GameObj = dropdown.gameObject
                });
            }
        }

        private static void AddButtonFromParent(GameObject panel, string parentName, string buttonName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var parent = panel.transform.Find(parentName);
            if (parent == null) return;

            var buttonTransform = parent.Find(buttonName);
            if (buttonTransform == null) return;

            var button = buttonTransform.GetComponent<Button>();
            if (button != null && button.gameObject.activeInHierarchy && !added.Contains(button.GetInstanceID()))
            {
                added.Add(button.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Button",
                    ButtonRef = button,
                    GameObj = button.gameObject
                });
            }
        }

        private static void AddButtonFromParentAllowInactive(GameObject panel, string parentName, string buttonName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var parent = panel.transform.Find(parentName);
            if (parent == null) return;

            var buttonTransform = parent.Find(buttonName);
            if (buttonTransform == null) return;

            var button = buttonTransform.GetComponent<Button>();
            if (button != null && !added.Contains(button.GetInstanceID()))
            {
                added.Add(button.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Button",
                    ButtonRef = button,
                    GameObj = button.gameObject
                });
            }
        }

        private static void AddButtonNestedParent(GameObject panel, string grandParentName, string parentName, string buttonName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var grandParent = panel.transform.Find(grandParentName);
            if (grandParent == null) return;

            var parent = grandParent.Find(parentName);
            if (parent == null) return;

            var buttonTransform = parent.Find(buttonName);
            if (buttonTransform == null) return;

            var button = buttonTransform.GetComponent<Button>();
            if (button != null && button.gameObject.activeInHierarchy && !added.Contains(button.GetInstanceID()))
            {
                added.Add(button.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Button",
                    ButtonRef = button,
                    GameObj = button.gameObject
                });
            }
        }

        private static void AddButtonNestedParentAllowInactive(GameObject panel, string grandParentName, string parentName, string buttonName, string label, List<MenuItem> items, HashSet<int> added)
        {
            var grandParent = panel.transform.Find(grandParentName);
            if (grandParent == null) return;

            var parent = grandParent.Find(parentName);
            if (parent == null) return;

            var buttonTransform = parent.Find(buttonName);
            if (buttonTransform == null) return;

            var button = buttonTransform.GetComponent<Button>();
            if (button != null && !added.Contains(button.GetInstanceID()))
            {
                added.Add(button.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Button",
                    ButtonRef = button,
                    GameObj = button.gameObject
                });
            }
        }

        private static void AddSliderItem(GameObject panel, string name, string label, List<MenuItem> items, HashSet<int> added)
        {
            var sliderTransform = FindRecursive(panel.transform, name);
            if (sliderTransform == null)
            {
                var allSliders = panel.GetComponentsInChildren<Slider>(true);
                foreach (var s in allSliders)
                {
                    if (s.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        sliderTransform = s.transform;
                        break;
                    }
                }
            }

            if (sliderTransform == null) return;

            var slider = sliderTransform.GetComponent<Slider>();
            if (slider != null && slider.gameObject.activeInHierarchy && !added.Contains(slider.GetInstanceID()))
            {
                added.Add(slider.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Slider",
                    SliderRef = slider,
                    GameObj = slider.gameObject
                });
            }
        }

        private static void AddAllSliders(GameObject panel, List<MenuItem> items, HashSet<int> added)
        {
            var allSliders = panel.GetComponentsInChildren<Slider>(true);
            int sliderCount = 0;
            foreach (var slider in allSliders)
            {
                if (slider.gameObject.activeInHierarchy && !added.Contains(slider.GetInstanceID()))
                {
                    added.Add(slider.GetInstanceID());
                    string label = $"Controle {sliderCount + 1}";
                    items.Add(new MenuItem
                    {
                        Label = label,
                        Category = "Slider",
                        SliderRef = slider,
                        GameObj = slider.gameObject
                    });
                    sliderCount++;
                }
            }
        }

        private static void AddToggleItem(GameObject panel, string name, string label, List<MenuItem> items, HashSet<int> added)
        {
            var toggleTransform = FindRecursive(panel.transform, name);
            if (toggleTransform == null)
            {
                var allToggles = panel.GetComponentsInChildren<Toggle>(true);
                foreach (var t in allToggles)
                {
                    if (t.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        toggleTransform = t.transform;
                        break;
                    }
                }
            }

            if (toggleTransform == null) return;

            var toggle = toggleTransform.GetComponent<Toggle>();
            if (toggle != null && toggle.gameObject.activeInHierarchy && !added.Contains(toggle.GetInstanceID()))
            {
                added.Add(toggle.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Toggle",
                    ToggleRef = toggle,
                    GameObj = toggle.gameObject
                });
            }
        }

        private static void AddDropdownItem(GameObject panel, string name, string label, List<MenuItem> items, HashSet<int> added)
        {
            var dropdownTransform = FindRecursive(panel.transform, name);
            if (dropdownTransform == null)
            {
                var allDropdowns = panel.GetComponentsInChildren<Dropdown>(true);
                foreach (var d in allDropdowns)
                {
                    if (d.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        dropdownTransform = d.transform;
                        break;
                    }
                }
            }

            if (dropdownTransform == null) return;

            var dropdown = dropdownTransform.GetComponent<Dropdown>();
            if (dropdown != null && dropdown.gameObject.activeInHierarchy && !added.Contains(dropdown.GetInstanceID()))
            {
                added.Add(dropdown.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Dropdown",
                    DropdownRef = dropdown,
                    GameObj = dropdown.gameObject
                });
            }
        }

        private static void AddButtonItem(GameObject panel, string name, string label, List<MenuItem> items, HashSet<int> added)
        {
            var buttonTransform = FindRecursive(panel.transform, name);
            if (buttonTransform == null)
            {
                var allButtons = panel.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        buttonTransform = b.transform;
                        break;
                    }
                }
            }

            if (buttonTransform == null) return;

            var button = buttonTransform.GetComponent<Button>();
            if (button != null && button.gameObject.activeInHierarchy && !added.Contains(button.GetInstanceID()))
            {
                added.Add(button.GetInstanceID());
                items.Add(new MenuItem
                {
                    Label = label,
                    Category = "Button",
                    ButtonRef = button,
                    GameObj = button.gameObject
                });
            }
        }

        private static void AddQuitButton(GameObject panel, List<MenuItem> items, HashSet<int> added)
        {
            var quitBtn = FindButtonByName(panel, "quit");
            if (quitBtn != null && quitBtn.gameObject.activeInHierarchy && !added.Contains(quitBtn.GetInstanceID()))
            {
                added.Add(quitBtn.GetInstanceID());
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
