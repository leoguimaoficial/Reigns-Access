using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Navegador para a tela de objetivos (Façanhas Reais / Royal Deeds).
    /// Acessada via: Menu Pausa > Aba Reino > Façanhas Reais
    /// Estrutura: Canvas/objectives/Viewport/Content/objectiveStatsElement(Clone)
    /// </summary>
    public class ObjectivesScreenNavigator : ScreenNavigatorBase
    {
        private Transform objectivesTransform;
        private List<Button> exitButtons = new List<Button>();
        
        public override string ScreenName => Localization.Get("royal_deeds");
        
        public override bool IsScreenActive()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Procurar painel "objectives"
            var objectives = canvas.transform.Find("objectives");
            if (objectives == null || !objectives.gameObject.activeInHierarchy) return false;
            
            // Verificar se tem o Viewport com conteúdo (estrutura real: objectives/Viewport/Content)
            var viewport = objectives.Find("Viewport");
            if (viewport == null || !viewport.gameObject.activeInHierarchy) return false;
            
            objectivesTransform = objectives;
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
            
            if (objectivesTransform == null) return;
            
            // Estrutura real: objectives/Viewport/Content/objectiveStatsElement(Clone)
            var viewport = objectivesTransform.Find("Viewport");
            if (viewport == null) return;
            
            var content = viewport.Find("Content");
            if (content == null) return;
            
            // Iterar pelos objetivos
            for (int i = 0; i < content.childCount; i++)
            {
                var objective = content.GetChild(i);
                string objectiveText = GetObjectiveText(objective, i + 1);
                
                if (!string.IsNullOrEmpty(objectiveText))
                {
                    texts.Add(objectiveText);
                }
            }
            
            // Adicionar botão SAIR
            CollectExitButton();
        }
        
        private string GetObjectiveText(Transform objective, int index)
        {
            // Buscar textos no objetivo
            var textComponents = objective.GetComponentsInChildren<Text>(true);
            
            string mainText = "";
            string nickname = "";
            bool isUnknown = false;
            
            foreach (var txt in textComponents)
            {
                if (txt == null) continue;
                
                string text = txt.text?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                
                string objName = txt.gameObject.name.ToLower();
                
                // Detectar texto principal do objetivo
                if (objName.Contains("objective") || objName.Contains("text") || objName.Contains("desc"))
                {
                    // Se contém "???", é objetivo não descoberto
                    if (text.Contains("???") || text.Contains("? ? ?"))
                    {
                        isUnknown = true;
                    }
                    else
                    {
                        mainText = text;
                    }
                }
                // Detectar nickname/apelido
                else if (objName.Contains("nick") || objName.Contains("title") || objName.Contains("name"))
                {
                    nickname = text;
                }
            }
            
            // Verificar se o objetivo está completado pela imagem do check
            bool isCompleted = IsObjectiveCompleted(objective);
            
            // Montar string de anúncio
            if (isUnknown)
            {
                if (!string.IsNullOrEmpty(nickname))
                {
                    return $"{Localization.Get("unknown_objective")}: {nickname}";
                }
                return $"{Localization.Get("unknown_objective")} {index}";
            }
            
            string result = "";
            if (!string.IsNullOrEmpty(mainText))
            {
                result = mainText;
            }
            
            if (!string.IsNullOrEmpty(nickname))
            {
                result += $". {Localization.Get("nickname_prefix")}{nickname}";
            }
            
            // Adicionar status de completado
            if (isCompleted)
            {
                result += $". {Localization.Get("completed")}";
            }
            else
            {
                result += $". {Localization.Get("not_completed")}";
            }
            
            return string.IsNullOrEmpty(result) ? $"{Localization.Get("unknown_objective")} {index}" : result;
        }
        
        /// <summary>
        /// Verifica se um objetivo está completado checando o campo 'state' do objeto Objective.
        /// state = "archived" significa completado
        /// state = "active" significa ativo mas não completado
        /// state = "hidden" significa oculto/bloqueado
        /// </summary>
        private bool IsObjectiveCompleted(Transform objective)
        {
            try
            {
                // Pegar todos os componentes e encontrar o ObjectiveBox
                var components = objective.GetComponents<Component>();
                Component objectiveBoxComp = null;
                
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name == "ObjectiveBox")
                    {
                        objectiveBoxComp = comp;
                        break;
                    }
                }
                
                if (objectiveBoxComp == null) return false;
                
                // Pegar o campo "obj" (Objective)
                var objField = objectiveBoxComp.GetType().GetField("obj", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (objField == null) return false;
                
                var objectiveObj = objField.GetValue(objectiveBoxComp);
                if (objectiveObj == null) return false;
                
                // Pegar o campo "state" (ObjectiveStates enum)
                var stateField = objectiveObj.GetType().GetField("state", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateField == null) return false;
                
                var stateValue = stateField.GetValue(objectiveObj);
                if (stateValue == null) return false;
                
                // Verificar se state == "archived" (completado)
                return stateValue.ToString().Equals("archived", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        private void CollectExitButton()
        {
            if (objectivesTransform == null) return;
            
            // Procurar botão de sair
            var buttons = objectivesTransform.GetComponentsInChildren<Button>(true);
            
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
