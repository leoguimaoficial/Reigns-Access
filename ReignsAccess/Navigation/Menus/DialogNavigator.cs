using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ReignsAccess.Accessibility;
using ReignsAccess.Navigation.Screens;

namespace ReignsAccess.Navigation.Menus
{
    /// <summary>
    /// Handles dialogs (confirmation popups).
    /// Dialogs have buttons like CANCELAR, OK, SIM, NÃO, SAIR.
    /// </summary>
    public static class DialogNavigator
    {
        private static bool _isDialogOpen = false;
        private static List<Button> _dialogButtons = new List<Button>();
        private static int _currentButtonIndex = 0;

        // Dialog button texts in all supported languages
        // These are used to detect if a dialog is open
        private static readonly string[] DialogButtonTexts = {
            // Portuguese
            "CANCELAR", "OK", "SIM", "NÃO", "FECHAR", "CONFIRMAR", "VOLTAR", "SAIR", "REINICIAR", "APLICAR", "CONTINUAR", "RETORNAR",
            // English
            "CANCEL", "YES", "NO", "CLOSE", "CONFIRM", "BACK", "EXIT", "RESTART", "APPLY", "CONTINUE", "RETURN", "QUIT",
            // Spanish
            "CANCELAR", "SÍ", "CERRAR", "CONFIRMAR", "VOLVER", "SALIR", "REINICIAR", "APLICAR", "CONTINUAR", "REGRESAR",
            // French
            "ANNULER", "OUI", "NON", "FERMER", "CONFIRMER", "RETOUR", "QUITTER", "REDÉMARRER", "APPLIQUER", "CONTINUER",
            // German
            "ABBRECHEN", "JA", "NEIN", "SCHLIESSEN", "BESTÄTIGEN", "ZURÜCK", "BEENDEN", "NEUSTART", "ANWENDEN", "WEITER",
            // Italian
            "ANNULLA", "SÌ", "CHIUDI", "CONFERMA", "INDIETRO", "ESCI", "RIAVVIA", "APPLICA", "CONTINUA",
            // Russian
            "ОТМЕНА", "ДА", "НЕТ", "ЗАКРЫТЬ", "ПОДТВЕРДИТЬ", "НАЗАД", "ВЫХОД", "ПЕРЕЗАПУСК", "ПРИМЕНИТЬ", "ПРОДОЛЖИТЬ",
            // Chinese
            "取消", "是", "否", "关闭", "确认", "返回", "退出", "重启", "应用", "继续",
            // Japanese
            "キャンセル", "はい", "いいえ", "閉じる", "確認", "戻る", "終了", "再起動", "適用", "続ける",
            // Korean
            "취소", "예", "아니오", "닫기", "확인", "뒤로", "종료", "다시 시작", "적용", "계속",
            // Polish
            "ANULUJ", "TAK", "NIE", "ZAMKNIJ", "POTWIERDŹ", "WRÓĆ", "WYJDŹ", "URUCHOM PONOWNIE", "ZASTOSUJ", "KONTYNUUJ",
            // Dutch
            "ANNULEREN", "JA", "NEE", "SLUITEN", "BEVESTIGEN", "TERUG", "AFSLUITEN", "HERSTARTEN", "TOEPASSEN", "DOORGAAN",
            // Turkish
            "İPTAL", "EVET", "HAYIR", "KAPAT", "ONAYLA", "GERİ", "ÇIKIŞ", "YENİDEN BAŞLAT", "UYGULA", "DEVAM"
        };

        public static bool IsDialogOpen => _isDialogOpen;

        /// <summary>
        /// Checks if a dialog is visible.
        /// A dialog is detected by having specific button texts visible.
        /// Must NOT be a pause menu (which has kingdombut, effectbut, optionsbut).
        /// </summary>
        public static bool IsDialogVisible()
        {
            // If pause menu is visible, it's not a dialog
            if (PauseMenuNavigator.IsPauseMenuVisible())
                return false;
            
            // If title screen is active, it's not a dialog
            if (TitleScreenNavigator.IsSpecialScreenActive())
                return false;

            // Look for a modal/dialog panel specifically
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Check for modal transform which contains dialogs
            var modal = canvas.transform.Find("modal");
            if (modal == null || !modal.gameObject.activeInHierarchy) return false;
            
            // Check if modal has active children with dialog buttons
            var buttons = modal.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (!btn.gameObject.activeInHierarchy || !btn.interactable) continue;

                var textComp = btn.GetComponentInChildren<Text>();
                if (textComp == null) continue;

                string text = textComp.text.Trim().ToUpper();
                
                // Check if it's a dialog button text
                foreach (var dialogText in DialogButtonTexts)
                {
                    if (text == dialogText)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Refreshes the list of dialog buttons.
        /// </summary>
        public static void RefreshButtons()
        {
            _dialogButtons.Clear();
            _currentButtonIndex = 0;
            
            // Search for modal in canvas
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var modal = canvas.transform.Find("modal");
            if (modal == null) return;

            var addedTexts = new HashSet<string>();
            var childButtons = modal.GetComponentsInChildren<Button>(true);
            
            foreach (var btn in childButtons)
            {
                if (!btn.gameObject.activeInHierarchy || !btn.interactable) continue;
                var textComp = btn.GetComponentInChildren<Text>();
                if (textComp == null) continue;
                string text = textComp.text.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                if (addedTexts.Contains(text.ToUpper())) continue;
                addedTexts.Add(text.ToUpper());
                _dialogButtons.Add(btn);
            }

            // Order buttons left-to-right (by world position x) to match UI layout
            _dialogButtons.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
}

        /// <summary>
        /// Called when a dialog opens.
        /// </summary>
        public static void OnDialogOpened()
        {
            if (_isDialogOpen) return;

            _isDialogOpen = true;
            RefreshButtons();
            AnnounceDialog();
        }

        /// <summary>
        /// Called when a dialog closes.
        /// </summary>
        public static void OnDialogClosed()
        {
            _isDialogOpen = false;
            _dialogButtons.Clear();
        }

        /// <summary>
        /// Updates dialog state based on visibility.
        /// </summary>
        public static void Update()
        {
            bool visible = IsDialogVisible();

            if (visible && !_isDialogOpen)
            {
                OnDialogOpened();
            }
            else if (!visible && _isDialogOpen)
            {
                OnDialogClosed();
            }
        }

        /// <summary>
        /// Announces the dialog content and buttons.
        /// </summary>
        public static void AnnounceDialog()
        {
            // Find dialog text
            string dialogText = FindDialogText();
            
            // Build button list
            var buttonNames = new List<string>();
            foreach (var btn in _dialogButtons)
            {
                var textComp = btn.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    buttonNames.Add(textComp.text.Trim());
                }
            }

            string announcement = Core.Localization.Get("dialog_default");
            if (!string.IsNullOrEmpty(dialogText))
            {
                announcement = dialogText;
            }

            if (buttonNames.Count > 0)
            {
                announcement += Core.Localization.Get("buttons_prefix") + string.Join(", ", buttonNames) + Core.Localization.Get("nav_hint");
            }

            TolkWrapper.Speak(announcement);

            // Announce first button
            if (_dialogButtons.Count > 0)
            {
                AnnounceCurrentButton();
            }
        }

        /// <summary>
        /// Finds the main dialog text (not button text).
        /// </summary>
        private static string FindDialogText()
        {
            var texts = UnityEngine.Object.FindObjectsOfType<Text>();
            string bestText = "";
            float largestSize = 0;

            foreach (var t in texts)
            {
                if (!t.gameObject.activeInHierarchy) continue;

                string text = t.text.Trim();
                if (string.IsNullOrEmpty(text) || text.Length < 5) continue;

                // Skip if it's a button text
                bool isButtonText = false;
                foreach (var btnText in DialogButtonTexts)
                {
                    if (text.ToUpper() == btnText)
                    {
                        isButtonText = true;
                        break;
                    }
                }
                if (isButtonText) continue;

                // Skip known UI elements
                string objName = t.gameObject.name.ToLower();
                if (objName.Contains("label") || objName.Contains("button")) continue;

                // Prefer larger text (likely the dialog message)
                if (t.fontSize > largestSize)
                {
                    largestSize = t.fontSize;
                    bestText = text;
                }
            }

            // Clean HTML tags
            bestText = System.Text.RegularExpressions.Regex.Replace(bestText, "<[^>]+>", "");
            return bestText.Trim();
        }

        /// <summary>
        /// Navigates to the next button.
        /// </summary>
        public static void NavigateRight()
        {
            if (!_isDialogOpen || _dialogButtons.Count == 0) return;

            _currentButtonIndex = (_currentButtonIndex + 1) % _dialogButtons.Count;
            AnnounceCurrentButton();
        }

        /// <summary>
        /// Navigates to the previous button.
        /// </summary>
        public static void NavigateLeft()
        {
            if (!_isDialogOpen || _dialogButtons.Count == 0) return;

            _currentButtonIndex = (_currentButtonIndex - 1 + _dialogButtons.Count) % _dialogButtons.Count;
            AnnounceCurrentButton();
        }

        /// <summary>
        /// Activates the current button.
        /// </summary>
        public static void Activate()
        {
            if (!_isDialogOpen || _dialogButtons.Count == 0) return;
            if (_currentButtonIndex >= _dialogButtons.Count) return;

            var btn = _dialogButtons[_currentButtonIndex];
            var textComp = btn.GetComponentInChildren<Text>();
            string btnText = textComp != null ? textComp.text : "botão";

            try
            {
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(btn.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                btn.onClick.Invoke();
                TolkWrapper.Speak(btnText + Core.Localization.Get("activated"));
}
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Dialog] Button click error: {ex.Message}");
            }
        }

        /// <summary>
        /// Announces the currently selected button.
        /// </summary>
        private static void AnnounceCurrentButton()
        {
            if (_dialogButtons.Count == 0 || _currentButtonIndex >= _dialogButtons.Count) return;

            var btn = _dialogButtons[_currentButtonIndex];
            var textComp = btn.GetComponentInChildren<Text>();
            string btnText = textComp != null ? textComp.text : Core.Localization.Get("button_fallback");

            TolkWrapper.Speak($"{btnText}, {_currentButtonIndex + 1}{Core.Localization.Get("position_of")}{_dialogButtons.Count}");
        }
    }
}
