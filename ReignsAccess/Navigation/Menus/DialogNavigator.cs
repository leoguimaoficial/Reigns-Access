using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;

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
        private static ModalAct _activeModal;

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
            
            // Look for a modal/dialog panel specifically
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            var modal = FindModalContainer(canvas.transform);
            if (modal == null || !modal.gameObject.activeInHierarchy) return false;

            _activeModal = modal.GetComponentsInChildren<ModalAct>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy);

            // Current Reigns notifications are ModalAct instances and may have no
            // labelled button at all. Treat the active instance as accessible UI.
            if (_activeModal != null)
            {
                return true;
            }
            
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

        private static Transform FindModalContainer(Transform canvas)
        {
            // Unity 6 uses "modals". Keep the singular name for older builds.
            return canvas.Find("modals") ?? canvas.Find("modal");
        }

        /// <summary>
        /// Refreshes the list of dialog buttons.
        /// </summary>
        public static void RefreshButtons()
        {
            _dialogButtons.Clear();
            _currentButtonIndex = 0;
            
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var modal = FindModalContainer(canvas.transform);
            if (modal == null) return;

            // ModalAct pop-ups are notifications, not choice dialogs. Their
            // supported action is ModalAct.Close(), even if the prefab exposes
            // an unlabeled backdrop Button internally.
            if (_activeModal != null) return;

            var addedButtons = new HashSet<int>();
            var childButtons = modal.GetComponentsInChildren<Button>(true);
            
            foreach (var btn in childButtons)
            {
                if (!btn.gameObject.activeInHierarchy || !btn.interactable) continue;
                if (!addedButtons.Add(btn.GetInstanceID())) continue;
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
            _activeModal = null;
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
            var dialogTexts = FindDialogTexts();
            
            // Build button list
            var buttonNames = new List<string>();
            foreach (var btn in _dialogButtons)
            {
                buttonNames.Add(GetButtonText(btn));
            }

            string announcement = Core.Localization.Get("dialog_default");
            if (dialogTexts.Count > 0)
            {
                announcement = string.Join(". ", dialogTexts);
            }

            if (buttonNames.Count > 0)
            {
                announcement += Core.Localization.Get("buttons_prefix") + string.Join(", ", buttonNames) + Core.Localization.Get("nav_hint");
            }
            else if (_activeModal != null)
            {
                announcement += Core.Localization.Get("modal_nav_hint");
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
        private static List<string> FindDialogTexts()
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var canvas = GameObject.Find("Canvas");
            var modal = canvas == null ? null : FindModalContainer(canvas.transform);
            if (modal == null) return result;

            foreach (var t in modal.GetComponentsInChildren<Text>(true))
            {
                if (!t.gameObject.activeInHierarchy) continue;

                string text = MenuHelpers.CleanText(t.text);
                if (string.IsNullOrEmpty(text)) continue;

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

                if (IsButtonTextComponent(t)) continue;
                if (seen.Add(text)) result.Add(text);
            }

            return result;
        }

        private static bool IsButtonTextComponent(Text text)
        {
            var current = text.transform;
            while (current != null)
            {
                if (current.GetComponent<Button>() != null) return true;
                current = current.parent;
            }
            return false;
        }

        private static string GetButtonText(Button button)
        {
            var textComp = button.GetComponentInChildren<Text>();
            var text = MenuHelpers.CleanText(textComp?.text);
            return string.IsNullOrEmpty(text) ? Core.Localization.Get("continue_button") : text;
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
            if (!_isDialogOpen) return;

            if (_dialogButtons.Count == 0)
            {
                if (_activeModal != null)
                {
                    TolkWrapper.Speak(Core.Localization.Get("advancing"));
                    _activeModal.Close();
                }
                return;
            }

            if (_currentButtonIndex >= _dialogButtons.Count) return;

            var btn = _dialogButtons[_currentButtonIndex];
            string btnText = GetButtonText(btn);

            try
            {
                btn.onClick.Invoke();
                TolkWrapper.Speak(btnText + Core.Localization.Get("activated"));
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Dialog] Button click error: {ex.Message}");
            }
        }

        public static void Repeat()
        {
            if (_isDialogOpen)
            {
                AnnounceDialog();
            }
        }

        /// <summary>
        /// Announces the currently selected button.
        /// </summary>
        private static void AnnounceCurrentButton()
        {
            if (_dialogButtons.Count == 0 || _currentButtonIndex >= _dialogButtons.Count) return;

            var btn = _dialogButtons[_currentButtonIndex];
            string btnText = GetButtonText(btn);

            TolkWrapper.Speak($"{btnText}, {_currentButtonIndex + 1}{Core.Localization.Get("position_of")}{_dialogButtons.Count}");
        }
    }
}
