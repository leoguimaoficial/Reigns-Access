using UnityEngine;
using UnityEngine.UI;
using ReignsAccess.Accessibility;
using ReignsAccess.Core;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Navegador para a tela de cronologia/timeline (após morte do rei).
    /// Exibe: ano atual, reinados anteriores, objetivos e botão NEXT.
    /// </summary>
    public class ChronoScreenNavigator : ScreenNavigatorBase
    {
        private Transform chronoTransform;
        private float lastCollectTime = 0f;
        private int lastCollectedCount = 0;
        
        public override string ScreenName => Localization.Get("chrono_screen");
        
        public override bool IsScreenActive()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            
            // Verificar se chrono está ativo
            var chrono = canvas.transform.Find("chrono");
            if (chrono == null || !chrono.gameObject.activeInHierarchy) return false;
            
            // Verificar se o game NÃO está ativo (se game estiver ativo, estamos jogando)
            var game = canvas.transform.Find("game");
            if (game != null && game.gameObject.activeInHierarchy) return false;
            
            // Verificar se title NÃO está ativo (não estamos na tela inicial)
            var title = canvas.transform.Find("title");
            if (title != null && title.gameObject.activeInHierarchy) return false;
            
            // Verificar se touch/action_touch está visível com "NEXT"
            var touch = canvas.transform.Find("touch");
            if (touch == null || !touch.gameObject.activeInHierarchy) return false;
            
            var actionTouch = touch.Find("action_touch");
            if (actionTouch == null || !actionTouch.gameObject.activeInHierarchy) return false;
            
            chronoTransform = chrono;
            return true;
        }
        
        public override void Update()
        {
            base.Update();
            
            // Recoletar automaticamente a cada 1 segundo para pegar elementos que aparecem por animação
            if (isActive && Time.time - lastCollectTime > 1f)
            {
                lastCollectTime = Time.time;
                int oldIndex = currentIndex;
                CollectTexts();
                
                // Se coletou novos itens, anunciar
                if (texts.Count > lastCollectedCount)
                {
                    lastCollectedCount = texts.Count;
                    // Manter índice atual se possível
                    currentIndex = oldIndex;
                    if (currentIndex >= texts.Count) currentIndex = texts.Count - 1;
                }
            }
        }
        
        protected override void OnScreenEnter()
        {
            isActive = true;
            lastCollectTime = Time.time;
            CollectTexts();
            lastCollectedCount = texts.Count;
            currentIndex = 0;
            
            if (texts.Count > 0)
            {
                AnnounceCurrentText();
            }
        }
        
        protected override void CollectTexts()
        {
            texts.Clear();
            
            if (chronoTransform == null) return;
            
            // 1. Coletar o ano atual
            CollectYear();
            
            // 2. Coletar reinados
            CollectReigns();
            
            // 3. Coletar objetivos
            CollectObjectives();
            
            // 4. Adicionar ação NEXT
            CollectNextAction();
        }
        
        private void CollectYear()
        {
            // Canvas/chrono/yearmask/year
            var yearmask = chronoTransform.Find("yearmask");
            if (yearmask == null) return;
            
            var yearObj = yearmask.Find("year");
            if (yearObj == null) return;
            
            var yearText = yearObj.GetComponent<Text>();
            if (yearText != null && !string.IsNullOrEmpty(yearText.text))
            {
                AddText(Localization.Get("year_prefix") + yearText.text);
            }
        }
        
        private void CollectReigns()
        {
            // Canvas/chrono/reigns contém os reign(Clone)
            var reigns = chronoTransform.Find("reigns");
            if (reigns == null) return;
            
            // Iterar pelos reign(Clone)
            int count = 0;
            for (int i = 0; i < reigns.childCount; i++)
            {
                var reign = reigns.GetChild(i);
                
                // Buscar textos dentro do reign
                // Tentar estruturas possíveis baseadas no diagnóstico
                Text kingText = null;
                Text yearText = null;
                
                // Buscar recursivamente por componentes Text APENAS ENABLED
                var allTexts = reign.GetComponentsInChildren<Text>(true);
                
                foreach (var txt in allTexts)
                {
                    // IMPORTANTE: Pegar apenas textos ENABLED para evitar pegar idiomas incorretos
                    if (!txt.enabled || string.IsNullOrEmpty(txt.text))
                        continue;
                    
                    // Heurística melhorada: 
                    // - yearsinpower geralmente contém "anos" ou números
                    // - king é geralmente um nome simples
                    string objName = txt.gameObject.name.ToLower();
                    
                    if (objName.Contains("year") || objName.Contains("power"))
                    {
                        // É o campo de anos
                        yearText = txt;
                    }
                    else if (objName.Contains("king") || objName.Contains("name"))
                    {
                        // É o nome do rei
                        kingText = txt;
                    }
                    else if (yearText == null && (txt.text.Contains("anos") || txt.text.Contains("years") || txt.text.Contains(" - ") || char.IsDigit(txt.text[0])))
                    {
                        // Fallback: se contém "anos" ou números, provavelmente é yearText
                        yearText = txt;
                    }
                    else if (kingText == null)
                    {
                        // Fallback: primeiro texto que sobrar é o nome
                        kingText = txt;
                    }
                }
                
                // Se encontrou algo, adicionar
                if (kingText != null || yearText != null)
                {
                    string info = "";
                    
                    // Se yearText já contém o nome (ex: "o Jovem\n16 anos"), usar só ele
                    if (yearText != null && yearText.text.Contains("\n"))
                    {
                        // Texto tem quebra de linha, provavelmente tem apelido + anos
                        info = yearText.text.Replace("\n", " "); // Remover quebra de linha
                        
                        // Se tem kingText diferente, prefixar com o nome
                        if (kingText != null && !yearText.text.Contains(kingText.text))
                        {
                            info = kingText.text + " " + info;
                        }
                    }
                    else
                    {
                        // Montagem normal
                        if (kingText != null) info = kingText.text;
                        if (yearText != null) 
                        {
                            if (!string.IsNullOrEmpty(info)) info += " ";
                            info += yearText.text;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(info))
                    {
                        AddText(info.Trim());
                        count++;
                    }
                }
            }
        }
        
        private void CollectObjectives()
        {
            // Canvas/fond/objectiveboxes
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var fond = canvas.transform.Find("fond");
            if (fond == null) return;
            
            var objectiveBoxes = fond.Find("objectiveboxes");
            if (objectiveBoxes == null) return;
            
            // Iterar pelos objetivos
            int count = 0;
            for (int i = 0; i < objectiveBoxes.childCount; i++)
            {
                var objective = objectiveBoxes.GetChild(i);
                
                // Buscar textos recursivamente APENAS ENABLED
                var texts = objective.GetComponentsInChildren<Text>(true);
                foreach (var txt in texts)
                {
                    // IMPORTANTE: Pegar apenas textos ENABLED para evitar pegar idiomas incorretos
                    if (txt.enabled && !string.IsNullOrEmpty(txt.text))
                    {
                        AddText(Localization.Get("objective_prefix") + txt.text.Trim());
                        count++;
                        break; // Apenas um texto por objetivo
                    }
                }
            }
        }
        
        private void CollectNextAction()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var touch = canvas.transform.Find("touch");
            if (touch == null) return;
            
            var actionTouch = touch.Find("action_touch");
            if (actionTouch == null) return;
            
            var txt = actionTouch.GetComponent<Text>();
            if (txt != null && !string.IsNullOrEmpty(txt.text))
            {
                AddText(txt.text.Trim());
            }
        }
        
        protected override void AnnounceCurrentText()
        {
            if (currentIndex >= 0 && currentIndex < texts.Count)
            {
                TolkWrapper.Speak(texts[currentIndex]);
            }
        }
    }
}
