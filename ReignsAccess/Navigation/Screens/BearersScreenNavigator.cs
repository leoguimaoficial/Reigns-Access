using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Navegador para a tela de personagens (Galeria de Retratos / Portrait Gallery).
    /// Acessada via: Menu Pausa > Aba Reino > Galeria de Retratos
    /// Estrutura: Canvas/bearers/Viewport/Content/bearerStatsElement(Clone)
    /// </summary>
    public class BearersScreenNavigator : ScreenNavigatorBase
    {
        private Transform bearersTransform;
        private List<Button> exitButtons = new List<Button>();
        
        public override string ScreenName => Localization.Get("portrait_gallery");
        
        public override bool IsScreenActive()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Procurar painel "bearers" (personagens/portadores)
            var bearers = canvas.transform.Find("bearers");
            if (bearers == null || !bearers.gameObject.activeInHierarchy) return false;
            
            // Verificar se tem o Viewport com conteúdo (estrutura real: bearers/Viewport/Content)
            var viewport = bearers.Find("Viewport");
            if (viewport == null || !viewport.gameObject.activeInHierarchy) return false;
            
            bearersTransform = bearers;
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
            
            if (bearersTransform == null) return;
            
            // Estrutura real: bearers/Viewport/Content/bearerStatsElement(Clone)
            var viewport = bearersTransform.Find("Viewport");
            if (viewport == null) return;
            
            var content = viewport.Find("Content");
            if (content == null) return;
            
            // Iterar pelos personagens
            for (int i = 0; i < content.childCount; i++)
            {
                var bearer = content.GetChild(i);
                string bearerName = GetBearerName(bearer, i + 1);
                
                if (!string.IsNullOrEmpty(bearerName))
                {
                    texts.Add(bearerName);
                }
            }
            
            // Adicionar botão SAIR
            CollectExitButton();
        }
        
        private string GetBearerName(Transform bearer, int index)
        {
            // Buscar textos no personagem
            var textComponents = bearer.GetComponentsInChildren<Text>(true);
            
            string characterName = "";
            string characterTitle = "";
            bool isUnknown = false;
            
            foreach (var txt in textComponents)
            {
                if (txt == null) continue;
                
                string text = txt.text?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                
                string objName = txt.gameObject.name.ToLower();
                
                // Se contém "???", é personagem não descoberto
                if (text.Contains("???") || text.Contains("? ? ?") || text == "?")
                {
                    isUnknown = true;
                    continue;
                }
                
                // Detectar nome do personagem
                if (objName.Contains("name") || objName.Contains("who") || objName.Contains("character"))
                {
                    characterName = text;
                }
                // Detectar título/papel
                else if (objName.Contains("title") || objName.Contains("role") || objName.Contains("job"))
                {
                    characterTitle = text;
                }
                // Se não tem nome específico, usar o primeiro texto encontrado
                else if (string.IsNullOrEmpty(characterName) && text.Length > 1)
                {
                    characterName = text;
                }
            }
            
            // Montar string de anúncio
            if (isUnknown && string.IsNullOrEmpty(characterName))
            {
                return $"{Localization.Get("unknown_character")} {index}";
            }
            
            string result = "";
            if (!string.IsNullOrEmpty(characterName))
            {
                result = characterName;
            }
            
            if (!string.IsNullOrEmpty(characterTitle))
            {
                if (!string.IsNullOrEmpty(result))
                {
                    result += $", {characterTitle}";
                }
                else
                {
                    result = characterTitle;
                }
            }
            
            return string.IsNullOrEmpty(result) ? $"{Localization.Get("unknown_character")} {index}" : result;
        }
        
        private void CollectExitButton()
        {
            if (bearersTransform == null) return;
            
            // Procurar botão de sair
            var buttons = bearersTransform.GetComponentsInChildren<Button>(true);
            
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
