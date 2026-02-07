using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Patches;
using ReignsAccess.Navigation.Menus;
using ReignsAccess.Navigation.Screens;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;
using ReignsAccess.GameData;

namespace ReignsAccess.Input
{
    /// <summary>
    /// Main keyboard input handler that routes to appropriate navigator.
    /// </summary>
    public class KeyboardNavigator : MonoBehaviour
    {
        private static KeyboardNavigator _instance;
        private float _lastInputTime = 0f;
        private const float INPUT_COOLDOWN = 0.05f; // 50ms - mais responsivo
        
        private float _lastEscapeTime = 0f;
        private const float ESCAPE_COOLDOWN = 0.3f; // 300ms para ESC/P - evita loops

        public static void Create(GameObject parent)
        {
            if (_instance == null)
            {
                _instance = parent.AddComponent<KeyboardNavigator>();
                CardNavigator.Initialize();
            }
        }

        public static void Destroy()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
            }
        }

        private void Update()
        {
            // F5 para recarregar mod completo (útil após mudança de idioma)
            if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
            {
                ModReloader.ReloadPlugin();
                return;
            }
            
            // Atualizar sistema de localização (verifica mudanças de idioma)
            Localization.Update();
            
            // IMPORTANTE: Atualizar TitleScreenNavigator PRIMEIRO para detectar telas especiais
            TitleScreenNavigator.Update();
            
            // Atualizar DeathScreenNavigator (tela de morte/game over)
            DeathScreenNavigator.Update();

            // Atualizar NarrativeScreenNavigator (telas de fala/narrativa)
            NarrativeScreenNavigator.Update();
            
            // DETECTAR ESC/P para controlar abertura do menu (com cooldown para evitar loops)
            if ((UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P)) && CheckEscapeCooldown())
            {
                if (!PauseMenuNavigator.IsMenuOpen && 
                    !QuitDialogNavigator.IsActive())
                {
                    // Usuário pressionou ESC/P e menu não está aberto - permitir abertura
                    PauseMenuNavigator.AllowMenuOpen();
                }
            }
            
            // Update menu state - detecção automática de abertura/fechamento
            QuitDialogNavigator.Update();
            DialogNavigator.Update();
            PauseMenuNavigator.Update();

            // Route input based on context
            // PRIORIDADE CORRETA: QuitDialog > Menu Principal > TitleScreen > DeathScreen > Narrative > Dialog > Gameplay
            if (QuitDialogNavigator.IsActive())
            {
                HandleQuitDialogInput();
            }
            else if (PauseMenuNavigator.IsMenuOpen || PauseMenuNavigator.IsMenuPanelVisible())
            {
                // Menu principal está aberto - ESC/Backspace aqui fecha completamente
                HandleMenuInput();
            }
            else if (TitleScreenNavigator.IsSpecialScreenActive())
            {
                HandleTitleScreenInput();
            }
            else if (DeathScreenNavigator.IsActive)
            {
                // Tela de morte ativa
                DeathScreenNavigator.HandleInput();
                UnityEngine.Input.ResetInputAxes();
                return;
            }
            else if (NarrativeScreenNavigator.IsActive)
            {
                // Tela narrativa está ativa - processar SOMENTE input dela
                // IMPORTANTE: processar ANTES de resetar os inputs
                NarrativeScreenNavigator.HandleInput();
                
                // Resetar input axes para impedir que o jogo processe as teclas
                UnityEngine.Input.ResetInputAxes();
                
                return; // Não processar mais nada
            }
            else if (DialogNavigator.IsDialogOpen)
            {
                HandleDialogInput();
            }
            else if (IsInGameplay())
            {
                HandleGameplayInput();
            }
        }

        /// <summary>
        /// Check if we're in actual gameplay (not title screen).
        /// Gameplay has: character card visible, stats visible, etc.
        /// </summary>
        private bool IsInGameplay()
        {
            // Look for gameplay-specific elements
            var texts = UnityEngine.Object.FindObjectsOfType<Text>();
            
            bool hasQuestion = false;
            bool hasCharacter = false;
            
            foreach (var t in texts)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                
                string objName = t.gameObject.name.ToLower();
                if (objName == "question" && !string.IsNullOrEmpty(t.text))
                    hasQuestion = true;
                if (objName == "who" && !string.IsNullOrEmpty(t.text))
                    hasCharacter = true;
            }

            return hasQuestion || hasCharacter;
        }

        private bool CheckCooldown()
        {
            if (Time.unscaledTime - _lastInputTime < INPUT_COOLDOWN)
                return false;
            _lastInputTime = Time.unscaledTime;
            return true;
        }

        private bool CheckEscapeCooldown()
        {
            if (Time.unscaledTime - _lastEscapeTime < ESCAPE_COOLDOWN)
                return false;
            _lastEscapeTime = Time.unscaledTime;
            return true;
        }

        private void HandleQuitDialogInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && CheckCooldown())
            {
                QuitDialogNavigator.NavigateLeft();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && CheckCooldown())
            {
                QuitDialogNavigator.NavigateRight();
            }
            else if ((UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)) && CheckCooldown())
            {
                QuitDialogNavigator.SelectCurrentButton();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && CheckCooldown())
            {
                QuitDialogNavigator.Close();
            }
        }

        private void HandleDialogInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && CheckCooldown())
            {
                DialogNavigator.NavigateLeft();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && CheckCooldown())
            {
                DialogNavigator.NavigateRight();
            }
            else if ((UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)) && CheckCooldown())
            {
                DialogNavigator.Activate();
            }
        }

        private void HandleMenuInput()
        {
            // Tab - switch tabs
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) && CheckCooldown())
            {
                PauseMenuNavigator.NextTab();
            }
            // Up - navigate items up
            else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) && CheckCooldown())
            {
                PauseMenuNavigator.NavigateUp();
            }
            // Down - navigate items down
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) && CheckCooldown())
            {
                PauseMenuNavigator.NavigateDown();
            }
            // Left - adjust value left
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && CheckCooldown())
            {
                PauseMenuNavigator.AdjustLeft();
            }
            // Right - adjust value right
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && CheckCooldown())
            {
                PauseMenuNavigator.AdjustRight();
            }
            // Enter - activate
            else if ((UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)) && CheckCooldown())
            {
                PauseMenuNavigator.Activate();
            }
            // Backspace ou ESC - fecha menu e volta para tela anterior
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace) && CheckCooldown())
            {
                PauseMenuNavigator.CloseMenu();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && CheckEscapeCooldown())
            {
                PauseMenuNavigator.CloseMenu();
            }
        }

        private void HandleTitleScreenInput()
        {
            // Navegação APENAS por setas VERTICAIS nas telas especiais
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) && CheckCooldown())
            {
                TitleScreenNavigator.NavigateDown();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) && CheckCooldown())
            {
                TitleScreenNavigator.NavigateUp();
            }
            else if ((UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)) && CheckCooldown())
            {
                TitleScreenNavigator.Activate();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.R) && CheckCooldown())
            {
                TitleScreenNavigator.RepeatCurrent();
            }
            // Backspace ou ESC - fecha tela especial
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace) && CheckCooldown())
            {
                TitleScreenNavigator.CloseCurrentScreen();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && CheckEscapeCooldown())
            {
                TitleScreenNavigator.CloseCurrentScreen();
            }
        }

        private void HandleGameplayInput()
        {
            // CRÍTICO: Bloquear gameplay se tela narrativa estiver ativa
            if (NarrativeScreenNavigator.IsActive)
            {
                return;
            }
            
            // Ignore gameplay input if quit dialog is active
            if (QuitDialogNavigator.IsActive())
                return;

            // Up Arrow - Read current card (character + question)
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) && CheckCooldown())
            {
                GameInfoReader.ReadCard();
            }
            // Down Arrow - Read all stats
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) && CheckCooldown())
            {
                GameInfoReader.ReadStats();
            }
            // Left Arrow - Swipe left (No option) directly
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && CheckCooldown())
            {
                CardNavigator.SwipeLeft();
            }
            // Right Arrow - Swipe right (Yes option) directly
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && CheckCooldown())
            {
                CardNavigator.SwipeRight();
            }
            // R - Repeat current card or intercale text
            else if (UnityEngine.Input.GetKeyDown(KeyCode.R) && CheckCooldown())
            {
                ReignsPatches.ReadCurrentCard();
            }
            // E - Read both options (without stat changes)
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E) && CheckCooldown())
            {
                GameInfoReader.ReadOptions();
            }
            // T - Read which stats will be affected
            else if (UnityEngine.Input.GetKeyDown(KeyCode.T) && CheckCooldown())
            {
                GameInfoReader.ReadAffectedStats();
            }
            // A - Read church stat
            else if (UnityEngine.Input.GetKeyDown(KeyCode.A) && CheckCooldown())
            {
                GameInfoReader.ReadStat("spiritual", Localization.Get("stat_church"));
            }
            // S - Read people stat
            else if (UnityEngine.Input.GetKeyDown(KeyCode.S) && CheckCooldown())
            {
                GameInfoReader.ReadStat("demography", Localization.Get("stat_people"));
            }
            // D - Read military stat
            else if (UnityEngine.Input.GetKeyDown(KeyCode.D) && CheckCooldown())
            {
                GameInfoReader.ReadStat("military", Localization.Get("stat_army"));
            }
            // F - Read treasury stat
            else if (UnityEngine.Input.GetKeyDown(KeyCode.F) && CheckCooldown())
            {
                GameInfoReader.ReadStat("treasure", Localization.Get("stat_treasury"));
            }
            // I - Read king info
            else if (UnityEngine.Input.GetKeyDown(KeyCode.I) && CheckCooldown())
            {
                GameInfoReader.ReadKingInfo();
            }
            // O - Read objective
            else if (UnityEngine.Input.GetKeyDown(KeyCode.O) && CheckCooldown())
            {
                GameInfoReader.ReadObjective();
            }
            // H - Help
            else if (UnityEngine.Input.GetKeyDown(KeyCode.H) && CheckCooldown())
            {
                GameInfoReader.ReadHelp();
            }
            // Q - Silence
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Q) && CheckCooldown())
            {
                TolkWrapper.Silence();
            }
            // Nota: ESC/P são tratados no início do Update() para garantir que _userRequestedOpen
            // seja marcado ANTES do PauseMenuNavigator.Update() detectar o painel ativo
        }
    }
}
