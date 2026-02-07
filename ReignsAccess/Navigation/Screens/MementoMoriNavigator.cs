using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Navegador para a tela de mortes (Memento Mori).
    /// Acessada via: Menu Pausa > Aba Reino > Memento Mori
    /// Estrutura: Canvas/deaths/Viewport/Content/portrait(Clone)
    /// NÃO CONFUNDIR com DeathScreenNavigator (tela "The King is Dead").
    /// </summary>
    public class MementoMoriNavigator : ScreenNavigatorBase
    {
        private Transform deathsTransform;
        private List<Button> exitButtons = new List<Button>();
        
        public override string ScreenName => Localization.Get("memento_mori");
        
        public override bool IsScreenActive()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Procurar painel "deaths"
            var deaths = canvas.transform.Find("deaths");
            if (deaths == null || !deaths.gameObject.activeInHierarchy) return false;
            
            // Verificar se tem o Viewport com conteúdo (estrutura real: deaths/Viewport/Content)
            var viewport = deaths.Find("Viewport");
            if (viewport == null || !viewport.gameObject.activeInHierarchy) return false;
            
            deathsTransform = deaths;
            return true;
        }
        
        protected override void OnScreenEnter()
        {
            isActive = true;
            CollectTexts();
            currentIndex = 0;
            
            if (texts.Count > 0)
            {
                string announce = $"{ScreenName}. {texts.Count}{Localization.Get("items_suffix")}";
                TolkWrapper.Speak(announce, interrupt: true);
                AnnounceCurrentText();
            }
        }
        
        protected override void CollectTexts()
        {
            texts.Clear();
            exitButtons.Clear();
            
            if (deathsTransform == null) return;
            
            // Estrutura real: deaths/Viewport/Content/portrait(Clone)
            var viewport = deathsTransform.Find("Viewport");
            if (viewport == null) return;
            
            var content = viewport.Find("Content");
            if (content == null) return;
            
            // Iterar pelos portraits
            for (int i = 0; i < content.childCount; i++)
            {
                var portrait = content.GetChild(i);
                string deathName = GetDeathName(portrait, i + 1);
                
                if (!string.IsNullOrEmpty(deathName))
                {
                    texts.Add(deathName);
                }
            }
            
            // Adicionar botão SAIR
            CollectExitButton();
        }
        
        private string GetDeathName(Transform portrait, int index)
        {
            // Buscar textos no portrait
            var textComponents = portrait.GetComponentsInChildren<Text>(true);
            
            foreach (var txt in textComponents)
            {
                if (txt == null || !txt.gameObject.activeInHierarchy) continue;
                
                string text = txt.text?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                
                // Se o texto é "? ? ?" ou similar, é morte não descoberta
                if (text.Contains("?"))
                {
                    return Localization.Get("unknown_death") + " " + index;
                }
                
                // Retornar o nome da morte
                return text;
            }
            
            // Se não encontrou texto, é morte não descoberta
            return Localization.Get("unknown_death") + " " + index;
        }
        
        private void CollectExitButton()
        {
            if (deathsTransform == null) return;
            
            // Procurar botão de sair (pode ser "quit", "exit", "back", etc.)
            var buttons = deathsTransform.GetComponentsInChildren<Button>(true);
            
            foreach (var btn in buttons)
            {
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                
                string name = btn.gameObject.name.ToLower();
                if (name.Contains("quit") || name.Contains("exit") || name.Contains("back") || name.Contains("close"))
                {
                    exitButtons.Add(btn);
                    texts.Add(Localization.Get("exit_button"));
                    return;
                }
                
                // Verificar texto do botão
                var textComp = btn.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    string btnText = textComp.text?.Trim().ToUpper();
                    if (btnText == "SAIR" || btnText == "EXIT" || btnText == "QUIT" || 
                        btnText == "VOLTAR" || btnText == "BACK" || btnText == "FECHAR" || btnText == "CLOSE")
                    {
                        exitButtons.Add(btn);
                        texts.Add(Localization.Get("exit_button"));
                        return;
                    }
                }
            }
        }
        
        protected override void ExecuteAction()
        {
            if (texts.Count == 0) return;
            
            // Se está no botão SAIR (último item)
            if (currentIndex == texts.Count - 1 && exitButtons.Count > 0)
            {
                TolkWrapper.Speak(Localization.Get("exit_button") + Localization.Get("activated"));
                exitButtons[0].onClick.Invoke();
                return;
            }
            
            // Para outros itens, não há ação (apenas visualização)
            TolkWrapper.Speak(texts[currentIndex] + Localization.Get("info_only"));
        }
        
        protected override void AnnounceCurrentText()
        {
            if (currentIndex >= 0 && currentIndex < texts.Count)
            {
                string text = texts[currentIndex];
                string announceWithPosition = $"{text}. {currentIndex + 1}{Localization.Get("position_of")}{texts.Count}";
                TolkWrapper.Speak(announceWithPosition);
            }
        }
        
        public override void CloseScreen()
        {
            if (exitButtons.Count > 0)
            {
                TolkWrapper.Speak(Localization.Get("closing_screen"));
                exitButtons[0].onClick.Invoke();
            }
        }
    }
}
