using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ReignsAccess.Accessibility;
using ReignsAccess.Navigation.Menus.Tabs;
using ReignsAccess.Core;
using static ReignsAccess.Navigation.Menus.MenuHelpers;

namespace ReignsAccess.Navigation.Menus
{
    /// <summary>
    /// Handles the pause menu navigation.
    /// Versão 8.0 - Refatorado com navegadores separados por aba.
    /// 
    /// O menu do jogo tem 3 painéis: "kingdom", "effects", "options"
    /// Cada painel é gerenciado por seu próprio navegador:
    /// - ReinoTabNavigator - Gerencia a aba Reino
    /// - EfeitosTabNavigator - Gerencia a aba Efeitos
    /// - OpcoesTabNavigator - Gerencia a aba Opções
    /// 
    /// Este coordenador apenas:
    /// - Detecta qual painel está ativo
    /// - Delega a construção de itens para o navegador apropriado
    /// - Gerencia navegação entre itens
    /// - Gerencia troca de abas
    /// </summary>
    public static class PauseMenuNavigator
    {
        // Estado do menu
        private static bool _isMenuActive = false;
        private static int _currentItemIndex = 0;
        private static List<MenuItem> _currentItems = new List<MenuItem>();
        
        // Painel atual
        private static GameObject _activePanel = null;
        private static string _activePanelName = "";
        
        // Para detectar retorno de sub-menus
        private static bool _wasInSubMenu = false;
        private static int _indexBeforeSubMenu = -1;
        private static float _menuClosedTime = 0f;
        private static float _lastMenuOpenTime = 0f;
        
        // FLAG PRINCIPAL: Menu foi fechado pelo usuário e só pode abrir com novo ESC/P
        private static bool _menuWasClosedByUser = false;
        
        // Nomes dos painéis válidos do menu
        private static readonly string[] ValidPanelNames = { "kingdom", "effects", "options" };
        
        // Flag para evitar detecção automática no início do jogo
        private static bool _gameStarted = false;
        private static float _gameStartTime = 0f;

        /// <summary>
        /// Verifica se o menu está aberto.
        /// </summary>
        public static bool IsMenuOpen
        {
            get { return _isMenuActive; }
        }

        /// <summary>
        /// Alias para IsMenuOpen.
        /// </summary>
        public static bool IsPauseMenuVisible()
        {
            return _isMenuActive;
        }

        /// <summary>
        /// Retorna o nome da aba atual em português.
        /// </summary>
        private static string GetCurrentTabName()
        {
            if (string.IsNullOrEmpty(_activePanelName)) return Localization.Get("tab_unknown");
            
            if (_activePanelName.Equals("kingdom", StringComparison.OrdinalIgnoreCase))
                return Localization.Get("tab_reino");
            if (_activePanelName.Equals("effects", StringComparison.OrdinalIgnoreCase))
                return Localization.Get("tab_efeitos");
            if (_activePanelName.Equals("options", StringComparison.OrdinalIgnoreCase))
                return Localization.Get("tab_opcoes");
            
            return _activePanelName;
        }

        /// <summary>
        /// Retorna o índice da aba atual (0=Reino, 1=Efeitos, 2=Opções).
        /// </summary>
        private static int GetCurrentTabIndex()
        {
            for (int i = 0; i < ValidPanelNames.Length; i++)
            {
                if (_activePanelName.Equals(ValidPanelNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 2; // Padrão: Opções
        }

        /// <summary>
        /// Procura por um painel de menu válido que esteja ativo.
        /// </summary>
        private static GameObject FindActiveMenuPanel()
        {
            foreach (var panelName in ValidPanelNames)
            {
                var obj = GameObject.Find(panelName);
                if (obj != null && obj.activeInHierarchy)
                {
                    // Verificar se tem os botões de aba
                    var kingdomBut = obj.transform.Find("kingdomBut");
                    var effectBut = obj.transform.Find("effectBut");
                    var optionsBut = obj.transform.Find("optionsBut");
                    
                    if (kingdomBut != null && effectBut != null && optionsBut != null)
                    {
                        return obj;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Verifica se algum painel de menu está VISUALMENTE ativo (verificação instantânea).
        /// Usado para priorização no KeyboardNavigator.
        /// Respeita a flag de fechamento pelo usuário.
        /// </summary>
        public static bool IsMenuPanelVisible()
        {
            // Se usuário fechou o menu, retornar false até que pressione ESC/P novamente
            if (_menuWasClosedByUser)
            {
                return false;
            }
            return FindActiveMenuPanel() != null;
        }

        /// <summary>
        /// Atualiza o estado do menu - chamado todo frame pelo KeyboardNavigator.
        /// LÓGICA SIMPLES: Menu só abre se usuário não tiver fechado antes (ou se pressionou ESC/P de novo).
        /// </summary>
        public static void Update()
        {
            // Não detectar menu nos primeiros 2 segundos do jogo
            if (!_gameStarted)
            {
                if (_gameStartTime == 0f)
                {
                    _gameStartTime = Time.time;
                }
                
                if (Time.time - _gameStartTime < 2f)
                {
                    return;
                }
                
                _gameStarted = true;
            }
            

            
            var activePanel = FindActiveMenuPanel();
            
            bool wasMenuActive = _isMenuActive;
            bool isMenuActiveNow = (activePanel != null);
            
            // Verificar se mudou de painel (trocou de aba)
            bool panelChanged = false;
            if (isMenuActiveNow && activePanel != _activePanel)
            {
                panelChanged = true;
            }
            
            if (isMenuActiveNow && !wasMenuActive)
            {
                // Menu está tentando abrir
                // SE o usuário fechou antes, NÃO permitir abertura automática
                if (_menuWasClosedByUser)
                {
                    // Usuário fechou o menu - ignorar até que pressione ESC/P
                    return;
                }
// Menu acabou de abrir
                _activePanel = activePanel;
                _activePanelName = activePanel.name;
                _isMenuActive = true;
                _lastMenuOpenTime = Time.unscaledTime;
                OnMenuOpened();
            }
            else if (!isMenuActiveNow && wasMenuActive)
            {
                // Menu acabou de fechar (pelo jogo, não por nós)
                // Isso acontece quando o jogo fecha o painel sozinho
                _indexBeforeSubMenu = _currentItemIndex;
                _wasInSubMenu = true;
                _menuClosedTime = Time.unscaledTime;
                
                _isMenuActive = false;
                _activePanel = null;
                _activePanelName = "";
                OnMenuClosed();
            }
            else if (panelChanged && isMenuActiveNow)
            {
                // Trocou de aba
                string oldPanelName = _activePanelName;
                _activePanel = activePanel;
                _activePanelName = activePanel.name;
                OnTabChanged(oldPanelName, _activePanelName);
            }
        }

        /// <summary>
        /// Chamado quando o menu abre.
        /// </summary>
        private static void OnMenuOpened()
        {
            // Verificar se foi fechamento temporário (sub-menu) ou real
            float timeSinceClosed = Time.unscaledTime - _menuClosedTime;
            bool isReturningFromSubMenu = _wasInSubMenu && timeSinceClosed < 5f; // 5 segundos para navegar sub-menus
            
            // Restaurar índice apenas se voltando de sub-menu (reabertura rápida)
            if (isReturningFromSubMenu && _indexBeforeSubMenu >= 0)
            {
                _currentItemIndex = _indexBeforeSubMenu;
            }
            else
            {
                _currentItemIndex = 0;
            }
            
            // Resetar flags
            _wasInSubMenu = false;
            _indexBeforeSubMenu = -1;
            
            _currentItems.Clear();
            BuildCurrentPanelItems();
            
            string tabName = GetCurrentTabName();
            
            // Anunciar diferente se voltando de sub-menu
            string announce;
            if (isReturningFromSubMenu)
            {
                announce = Localization.Get("tab_prefix") + tabName + ".";
            }
            else
            {
                announce = Localization.Get("pause_menu_opened") + tabName + ". " + _currentItems.Count + Localization.Get("items_suffix");
            }
            
            TolkWrapper.Speak(announce, interrupt: true);
            
            if (_currentItems.Count > 0)
            {
                AnnounceCurrentItem();
            }
        }

        /// <summary>
        /// Chamado quando o menu fecha.
        /// </summary>
        private static void OnMenuClosed()
        {
            _currentItems.Clear();
            
            // Anunciar apenas se não for abertura de sub-menu (esperar um pouco)
            // O sub-menu reabre o menu rápido, então não anunciamos ainda
        }

        /// <summary>
        /// Chamado quando o usuário troca de aba.
        /// </summary>
        private static void OnTabChanged(string oldPanel, string newPanel)
        {
            _currentItemIndex = 0;
            _currentItems.Clear();
            BuildCurrentPanelItems();
            
            string tabName = GetCurrentTabName();
            string announcement = Localization.Get("tab_prefix") + tabName + ". " + _currentItems.Count + Localization.Get("items_suffix") + Localization.Get("tab_nav_hint");
            TolkWrapper.Speak(announcement, interrupt: true);
            
            // Não anunciar o primeiro item automaticamente - deixar o usuário navegar
        }

        /// <summary>
        /// Constrói os itens do painel atual delegando para o navegador apropriado.
        /// </summary>
        private static void BuildCurrentPanelItems()
        {
            _currentItems.Clear();
            
            if (_activePanel == null) return;

            int tabIndex = GetCurrentTabIndex();
            
            // Delegar para o navegador de aba apropriado
            switch (tabIndex)
            {
                case 0: // Reino
                    _currentItems = ReinoTabNavigator.BuildItems(_activePanel);
                    break;
                case 1: // Efeitos
                    _currentItems = EfeitosTabNavigator.BuildItems(_activePanel);
                    break;
                case 2: // Opções
                    _currentItems = OpcoesTabNavigator.BuildItems(_activePanel);
                    break;
            }

            if (_currentItems.Count == 0)
            {
                _currentItems.Add(new MenuItem { Label = Localization.Get("no_items") });
            }
        }

        /// <summary>
        /// Fecha o menu de pausa e volta para a tela anterior.
        /// </summary>
        public static void CloseMenu()
        {
            if (!_isMenuActive || _activePanel == null) return;
            
            _menuWasClosedByUser = true;
            
            var panelRef = _activePanel;
            
            // Limpar estado do menu
            _isMenuActive = false;
            _activePanel = null;
            _activePanelName = "";
            _currentItems.Clear();
            
            // Limpar navegação de sub-menu também (sempre limpa ao fechar)
            _wasInSubMenu = false;
            _indexBeforeSubMenu = -1;
            
            // SEMPRE clicar no botão quit primeiro (garante fechamento correto)
            var quitBtn = FindButtonByName(panelRef, "quit");
            if (quitBtn != null && quitBtn.interactable)
            {
                try
                {
                    quitBtn.onClick.Invoke();
}
                catch (System.Exception ex)
                {
                    Plugin.Logger.LogError($"[Menu] Erro ao clicar quit: {ex.Message}");
                }
            }
            else
            {
                // Se não achou o botão quit, tentar desativar painel diretamente
try
                {
                    panelRef.SetActive(false);
}
                catch (System.Exception ex)
                {
                    Plugin.Logger.LogError($"[Menu] Erro ao desativar painel: {ex.Message}");
                }
            }
            
            TolkWrapper.Speak(Localization.Get("menu_closed"));
        }

        /// <summary>
        /// Permite que o menu seja aberto novamente.
        /// Chamado quando usuário explicitamente pressiona ESC/P.
        /// </summary>
        public static void AllowMenuOpen()
        {
            _menuWasClosedByUser = false;
}

        /// <summary>
        /// Tab - Clica no próximo botão de aba para mudar de aba.
        /// </summary>
        public static void NextTab()
        {
            if (!_isMenuActive || _activePanel == null) return;
            
            int currentIndex = GetCurrentTabIndex();
            int nextIndex = (currentIndex + 1) % ValidPanelNames.Length;
            
            string nextTabButtonName = "";
            string nextTabName = "";
            
            switch (nextIndex)
            {
                case 0: 
                    nextTabButtonName = "kingdomBut"; 
                    nextTabName = Localization.Get("tab_reino"); 
                    break;
                case 1: 
                    nextTabButtonName = "effectBut"; 
                    nextTabName = Localization.Get("tab_efeitos"); 
                    break;
                case 2: 
                    nextTabButtonName = "optionsBut"; 
                    nextTabName = Localization.Get("tab_opcoes"); 
                    break;
            }
            
            var btnTransform = _activePanel.transform.Find(nextTabButtonName);
            if (btnTransform != null)
            {
                var btn = btnTransform.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    try
                    {
                        btn.onClick.Invoke();
                        // OnTabChanged vai anunciar a nova aba
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"[Menu] Erro ao clicar aba: {ex.Message}");
                        TolkWrapper.Speak(Localization.Get("tab_switch_error"));
                    }
                }
                else
                {
                    TolkWrapper.Speak(Localization.Get("tab_not_available"));
                }
            }
            else
            {
                TolkWrapper.Speak(Localization.Get("tab_not_found"));
            }
        }

        /// <summary>
        /// Navega para cima.
        /// </summary>
        public static void NavigateUp()
        {
            if (!_isMenuActive || _currentItems.Count == 0) return;
            
            _currentItemIndex = (_currentItemIndex - 1 + _currentItems.Count) % _currentItems.Count;
            AnnounceCurrentItem();
        }

        /// <summary>
        /// Navega para baixo.
        /// </summary>
        public static void NavigateDown()
        {
            if (!_isMenuActive || _currentItems.Count == 0) return;
            
            _currentItemIndex = (_currentItemIndex + 1) % _currentItems.Count;
            AnnounceCurrentItem();
        }

        /// <summary>
        /// Anuncia o item atual.
        /// </summary>
        private static void AnnounceCurrentItem()
        {
            if (_currentItemIndex >= _currentItems.Count) return;
            
            var item = _currentItems[_currentItemIndex];
            string valueInfo = "";
            
            if (item.SliderRef != null)
            {
                valueInfo = $": {Mathf.RoundToInt(item.SliderRef.normalizedValue * 100)}%";
            }
            else if (item.ToggleRef != null)
            {
                valueInfo = item.ToggleRef.isOn ? Localization.Get("toggle_on") : Localization.Get("toggle_off");
            }
            else if (item.DropdownRef != null && item.DropdownRef.options.Count > 0)
            {
                valueInfo = $": {item.DropdownRef.options[item.DropdownRef.value].text}";
            }
            
            string announce = $"{item.Label}{valueInfo}. {_currentItemIndex + 1}{Localization.Get("position_of")}{_currentItems.Count}";
            TolkWrapper.Speak(announce);
        }

        /// <summary>
        /// Ajusta valor para a esquerda.
        /// </summary>
        public static void AdjustLeft()
        {
            if (!_isMenuActive || _currentItems.Count == 0 || _currentItemIndex >= _currentItems.Count) return;
            
            var item = _currentItems[_currentItemIndex];
            
            if (item.SliderRef != null)
            {
                float step = (item.SliderRef.maxValue - item.SliderRef.minValue) * 0.1f;
                item.SliderRef.value = Mathf.Max(item.SliderRef.minValue, item.SliderRef.value - step);
                TolkWrapper.Speak($"{Mathf.RoundToInt(item.SliderRef.normalizedValue * 100)}%");
            }
            else if (item.DropdownRef != null && item.DropdownRef.value > 0)
            {
                item.DropdownRef.value--;
                TolkWrapper.Speak(item.DropdownRef.options[item.DropdownRef.value].text);
            }
        }

        /// <summary>
        /// Ajusta valor para a direita.
        /// </summary>
        public static void AdjustRight()
        {
            if (!_isMenuActive || _currentItems.Count == 0 || _currentItemIndex >= _currentItems.Count) return;
            
            var item = _currentItems[_currentItemIndex];
            
            if (item.SliderRef != null)
            {
                float step = (item.SliderRef.maxValue - item.SliderRef.minValue) * 0.1f;
                item.SliderRef.value = Mathf.Min(item.SliderRef.maxValue, item.SliderRef.value + step);
                TolkWrapper.Speak($"{Mathf.RoundToInt(item.SliderRef.normalizedValue * 100)}%");
            }
            else if (item.DropdownRef != null && item.DropdownRef.value < item.DropdownRef.options.Count - 1)
            {
                item.DropdownRef.value++;
                TolkWrapper.Speak(item.DropdownRef.options[item.DropdownRef.value].text);
            }
        }

        /// <summary>
        /// Ativa o item atual.
        /// </summary>
        public static void Activate()
        {
            if (!_isMenuActive || _currentItems.Count == 0 || _currentItemIndex >= _currentItems.Count) return;
            
            var item = _currentItems[_currentItemIndex];
            
            if (item.ToggleRef != null)
            {
                item.ToggleRef.isOn = !item.ToggleRef.isOn;
                TolkWrapper.Speak(item.ToggleRef.isOn ? Localization.Get("toggle_on").TrimStart(':').Trim() : Localization.Get("toggle_off").TrimStart(':').Trim());
            }
            else if (item.ButtonRef != null)
            {
                try
                {
                    
                    if (!item.ButtonRef.interactable)
                    {
                        TolkWrapper.Speak(item.Label + Localization.Get("not_available"));
                        return;
                    }
                    
                    item.ButtonRef.onClick.Invoke();
                    TolkWrapper.Speak(item.Label + Localization.Get("activated"));
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[Menu] Erro ao clicar {item.Label}: {ex.Message}");
                    TolkWrapper.Speak(Localization.Get("activation_error") + item.Label);
                }
            }
            else if (item.SliderRef != null)
            {
                TolkWrapper.Speak(Localization.Get("slider_hint") + $"{Mathf.RoundToInt(item.SliderRef.normalizedValue * 100)}%");
            }
            else if (item.DropdownRef != null)
            {
                TolkWrapper.Speak(Localization.Get("slider_hint") + item.DropdownRef.options[item.DropdownRef.value].text);
            }
            else if (item.Category == "Info")
            {
                TolkWrapper.Speak(item.Label + Localization.Get("info_only"));
            }
            else
            {
                TolkWrapper.Speak(item.Label + Localization.Get("no_action"));
            }
        }
    }
}
