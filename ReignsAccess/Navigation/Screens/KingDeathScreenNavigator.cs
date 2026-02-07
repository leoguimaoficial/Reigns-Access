using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Navegador para a tela de morte do rei (King Death Screen).
    /// Aparece quando o rei morre, mostrando "O Rei está morto", anos de reinado e nome.
    /// Estrutura: Canvas/title/deadking
    /// </summary>
    public class KingDeathScreenNavigator : ScreenNavigatorBase
    {
        private Transform deadkingTransform;
        private Button advanceButton;
        
        public override string ScreenName => "King Death Screen";
        
        public override bool IsScreenActive()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Verificar se title está ativo
            var title = canvas.transform.Find("title");
            if (title == null || !title.gameObject.activeInHierarchy) return false;
            
            // Verificar se deadking está ativo
            var deadking = title.Find("deadking");
            if (deadking == null || !deadking.gameObject.activeInHierarchy) return false;
            
            // Verificar se game NÃO está ativo (não estamos jogando)
            var game = canvas.transform.Find("game");
            if (game != null && game.gameObject.activeInHierarchy) return false;
            
            // Verificar se o botão AVANÇAR está presente
            var touch = canvas.transform.Find("touch");
            if (touch == null || !touch.gameObject.activeInHierarchy) return false;
            
            var actionTouch = touch.Find("action_touch");
            if (actionTouch == null || !actionTouch.gameObject.activeInHierarchy) return false;
            
            deadkingTransform = deadking;
            return true;
        }
        
        protected override void OnScreenEnter()
        {
            isActive = true;
            CollectTexts();
            currentIndex = 0;
            
            if (texts.Count > 0)
            {
                // Anunciar o primeiro item (texto principal)
                AnnounceCurrentText();
            }
        }
        
        protected override void CollectTexts()
        {
            texts.Clear();
            advanceButton = null;
            
            if (deadkingTransform == null) return;
            
            // 1. Coletar texto principal: "O Rei está morto"
            CollectMainText();
            
            // 2. Coletar anos de reinado: "603 - 608"
            CollectYears();
            
            // 3. Coletar nome do rei: "Baudouin"
            CollectKingName();
            
            // 4. Coletar botão AVANÇAR
            CollectAdvanceButton();
        }
        
        private void CollectMainText()
        {
            // Concatenar: the + king + isdead
            // Exemplo: "O" + "Rei" + "está morto" = "O Rei está morto"
            
            string the = GetTextFromChild("the");
            string king = GetTextFromChild("king");
            string isdead = GetTextFromChild("isdead");
            
            // Montar o texto completo
            string mainText = "";
            if (!string.IsNullOrEmpty(the)) mainText += the + " ";
            if (!string.IsNullOrEmpty(king)) mainText += king + " ";
            if (!string.IsNullOrEmpty(isdead)) mainText += isdead;
            
            if (!string.IsNullOrEmpty(mainText.Trim()))
            {
                AddText(mainText.Trim());
            }
        }
        
        private void CollectYears()
        {
            // Canvas/title/deadking/inpower: "603 - 608"
            string years = GetTextFromChild("inpower");
            
            if (!string.IsNullOrEmpty(years))
            {
                AddText(years);
            }
        }
        
        private void CollectKingName()
        {
            // Canvas/title/deadking/highscore: "Baudouin"
            string kingName = GetTextFromChild("highscore");
            
            if (!string.IsNullOrEmpty(kingName))
            {
                AddText(kingName);
            }
        }
        
        private void CollectAdvanceButton()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var touch = canvas.transform.Find("touch");
            if (touch == null) return;
            
            // Pegar texto do botão
            var actionTouch = touch.Find("action_touch");
            if (actionTouch != null)
            {
                var txt = actionTouch.GetComponent<Text>();
                if (txt != null && !string.IsNullOrEmpty(txt.text))
                {
                    AddText(txt.text.Trim());
                }
            }
            
            // Pegar o componente Button
            var but = touch.Find("but");
            if (but != null)
            {
                advanceButton = but.GetComponent<Button>();
            }
        }
        
        private string GetTextFromChild(string childName)
        {
            if (deadkingTransform == null) return "";
            
            var child = deadkingTransform.Find(childName);
            if (child == null) return "";
            
            var txt = child.GetComponent<Text>();
            if (txt != null && txt.enabled && !string.IsNullOrEmpty(txt.text))
            {
                return txt.text.Trim();
            }
            
            return "";
        }
        
        protected override void ExecuteAction()
        {
            if (texts.Count == 0) return;
            
            // Se está no último item (botão AVANÇAR)
            if (currentIndex == texts.Count - 1 && advanceButton != null)
            {
                TolkWrapper.Speak(Localization.Get("activated"));
                advanceButton.onClick.Invoke();
                return;
            }
            
            // Para outros itens, apenas re-anunciar
            AnnounceCurrentText();
        }
        
        protected override void AnnounceCurrentText()
        {
            if (currentIndex >= 0 && currentIndex < texts.Count)
            {
                string text = texts[currentIndex];
                string announceWithPosition = $"{text}. {currentIndex + 1} {Localization.Get("position_of")} {texts.Count}";
                TolkWrapper.Speak(announceWithPosition);
            }
        }
    }
}
