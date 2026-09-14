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
    /// Navegador para a tela de cronologia/timeline (após morte do rei).
    /// Exibe: ano atual, reinados anteriores, objetivos e botão NEXT.
    /// </summary>
    public class ChronoScreenNavigator : ScreenNavigatorBase
    {
        private const int DefaultTimelineStartYear = 603;
        private const float MaximumCluePairDistance = 80f;

        private sealed class TimelineClue
        {
            public int Year;
            public string SymbolKey;
            public bool KingFacesSymbol;
        }

        private Transform chronoTransform;
        private float lastCollectTime = 0f;
        private int lastCollectedCount = 0;
        private string lastNewKingAnnouncement = "";
        
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

            // The final succession title lives outside Canvas/chrono and only
            // remains visible briefly. Watch it every frame so the screen reader
            // does not miss "Long live" and the new monarch's name.
            if (isActive)
            {
                AnnounceNewKingWhenShown();
            }
            
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
            lastNewKingAnnouncement = GetNewKingAnnouncement();
            
            if (texts.Count > 0)
            {
                AnnounceCurrentText();
            }
        }

        protected override void OnScreenExit()
        {
            base.OnScreenExit();
            lastNewKingAnnouncement = "";
        }
        
        protected override void CollectTexts()
        {
            texts.Clear();
            
            if (chronoTransform == null) return;

            // This is Canvas/fond/newking, not a child of the chronology panel.
            // It is the last title displayed before gameplay resumes.
            CollectNewKingAnnouncement();
            
            // 1. Coletar o ano atual
            CollectYear();

            // 2. Coletar as pistas visuais da cronologia, incluindo a orientação do rei
            CollectTimelineClues();
            
            // 3. Coletar reinados
            CollectReigns();
            
            // 4. Coletar objetivos
            CollectObjectives();
            
            // 5. Adicionar ação NEXT
            CollectNextAction();
        }

        private void CollectNewKingAnnouncement()
        {
            string announcement = GetNewKingAnnouncement();
            if (!string.IsNullOrEmpty(announcement))
            {
                AddText(announcement);
            }
        }

        private string GetNewKingAnnouncement()
        {
            var canvas = GameObject.Find("Canvas");
            var panel = canvas?.transform.Find("fond/newking");
            if (panel == null || !panel.gameObject.activeInHierarchy)
                return "";

            var parts = new List<string>();
            AddVisibleText(panel.Find("longlive"), parts);
            AddVisibleText(panel.Find("king_young"), parts);

            // king_young is only the localized word "King" in the current PC
            // assets. The actual monarch name is stored in the final live
            // ChronoReign (for example, "Jorge").
            string currentKingName = GetCurrentKingName();
            if (!string.IsNullOrEmpty(currentKingName) && !parts.Contains(currentKingName))
                parts.Add(currentKingName);

            // Asset variants can rename the king label. Fall back to every
            // visible text under the panel without repeating the two known ones.
            if (parts.Count < 2)
            {
                foreach (var text in panel.GetComponentsInChildren<Text>(true))
                {
                    if (!text.enabled || !text.gameObject.activeInHierarchy)
                        continue;

                    string value = CleanScreenText(text.text);
                    if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
                        parts.Add(value);
                }
            }

            return string.Join(" ", parts);
        }

        private string GetCurrentKingName()
        {
            var chronoAct = chronoTransform?.GetComponent<ChronoAct>()
                ?? chronoTransform?.GetComponentInChildren<ChronoAct>(true);
            if (chronoAct?.scReigns == null)
                return "";

            for (int i = chronoAct.scReigns.Count - 1; i >= 0; i--)
            {
                var reign = chronoAct.scReigns[i];
                if (reign?.kingname == null)
                    continue;

                string value = CleanScreenText(reign.kingname.text);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return "";
        }

        private static void AddVisibleText(Transform item, List<string> parts)
        {
            if (item == null || !item.gameObject.activeInHierarchy)
                return;

            var text = item.GetComponent<Text>();
            if (text == null || !text.enabled)
                return;

            string value = CleanScreenText(text.text);
            if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
                parts.Add(value);
        }

        private static string CleanScreenText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return System.Text.RegularExpressions.Regex
                .Replace(value, "<[^>]*>", "")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private void AnnounceNewKingWhenShown()
        {
            string announcement = GetNewKingAnnouncement();
            if (string.IsNullOrEmpty(announcement))
            {
                lastNewKingAnnouncement = "";
                return;
            }

            if (announcement == lastNewKingAnnouncement)
                return;

            lastNewKingAnnouncement = announcement;
            CollectTexts();
            lastCollectedCount = texts.Count;
            currentIndex = 0;
            Plugin.Logger.LogInfo($"[ChronoScreen] New king title: {announcement}");
            TolkWrapper.Speak(announcement, interrupt: true);
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

        /// <summary>
        /// Lê as pistas do caminho do Diabo que o jogo desenha na cronologia.
        /// Cada pista tem um símbolo e uma silhueta do rei próxima. O sinal da
        /// escala horizontal da silhueta indica para que lado o rei está olhando.
        /// </summary>
        private void CollectTimelineClues()
        {
            var scenes = chronoTransform.Find("scenes");
            var devilPath = scenes?.Find("devilpath");
            if (devilPath == null || !devilPath.gameObject.activeInHierarchy)
                return;

            var kings = new List<Transform>();
            var symbols = new List<Transform>();
            var symbolKeys = new Dictionary<Transform, string>();

            for (int i = 0; i < devilPath.childCount; i++)
            {
                var child = devilPath.GetChild(i);
                if (!IsVisibleTimelineGraphic(child))
                    continue;

                string objectName = NormalizeTimelineObjectName(child.name);
                if (objectName == "perso")
                {
                    kings.Add(child);
                    continue;
                }

                string symbolKey = GetTimelineSymbolKey(objectName);
                if (symbolKey != null)
                {
                    symbols.Add(child);
                    symbolKeys[child] = symbolKey;
                }
            }

            var clues = new List<TimelineClue>();
            var pairedKings = new HashSet<Transform>();
            int startYear = GetTimelineStartYear();
            int latestYear = GetLatestTimelineYear(startYear);

            foreach (var symbol in symbols)
            {
                Transform nearestKing = null;
                float nearestDistance = float.MaxValue;
                float symbolX = GetTimelineX(symbol);

                foreach (var king in kings)
                {
                    if (pairedKings.Contains(king))
                        continue;

                    float distance = Mathf.Abs(GetTimelineX(king) - symbolX);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestKing = king;
                    }
                }

                if (nearestKing == null || nearestDistance > MaximumCluePairDistance)
                    continue;

                int clueYear = startYear + Mathf.RoundToInt(symbolX / 10f) - 1;
                if (clueYear > latestYear)
                    continue;

                pairedKings.Add(nearestKing);
                clues.Add(new TimelineClue
                {
                    Year = clueYear,
                    SymbolKey = symbolKeys[symbol],
                    KingFacesSymbol = IsKingFacingSymbol(nearestKing, symbol)
                });
            }

            clues.Sort((left, right) => left.Year.CompareTo(right.Year));
            foreach (var clue in clues)
            {
                string orientationKey = clue.KingFacesSymbol
                    ? "king_facing_symbol"
                    : "king_back_to_symbol";

                AddText(Localization.Get(
                    "timeline_clue",
                    clue.Year,
                    Localization.Get(clue.SymbolKey),
                    Localization.Get(orientationKey)));
            }
        }

        private static bool IsVisibleTimelineGraphic(Transform item)
        {
            if (item == null || !item.gameObject.activeInHierarchy)
                return false;

            var graphic = item.GetComponent<Graphic>();
            return graphic == null || (graphic.enabled && graphic.color.a > 0.01f);
        }

        private static string NormalizeTimelineObjectName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return string.Empty;

            int cloneSuffix = objectName.IndexOf('(');
            if (cloneSuffix >= 0)
                objectName = objectName.Substring(0, cloneSuffix);

            return objectName.Trim().ToLowerInvariant();
        }

        private static string GetTimelineSymbolKey(string objectName)
        {
            switch (objectName)
            {
                case "feu":
                    return "timeline_symbol_fire";
                case "arsenic":
                    return "timeline_symbol_arsenic";
                case "acide":
                    return "timeline_symbol_acid";
                case "gold":
                    return "timeline_symbol_gold";
                case "notdevil":
                case "devil":
                    return "timeline_symbol_pentagram";
                default:
                    return null;
            }
        }

        private int GetTimelineStartYear()
        {
            return GetChronoYearField("start", DefaultTimelineStartYear);
        }

        private int GetLatestTimelineYear(int startYear)
        {
            int displayedYear = GetDisplayedTimelineYear(startYear);
            return GetChronoYearField("lastyear", displayedYear);
        }

        private int GetDisplayedTimelineYear(int fallback)
        {
            var yearText = chronoTransform
                .Find("yearmask")?
                .Find("year")?
                .GetComponent<Text>();

            int year;
            return yearText != null && int.TryParse(yearText.text?.Trim(), out year)
                ? year
                : fallback;
        }

        private int GetChronoYearField(string fieldName, int fallback)
        {
            try
            {
                var chronoAct = chronoTransform.GetComponent<ChronoAct>()
                    ?? chronoTransform.GetComponentInChildren<ChronoAct>(true);
                var field = typeof(ChronoAct).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (chronoAct != null && field != null)
                    return (int)field.GetValue(chronoAct);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[ChronoScreen] Could not read {fieldName}: {ex.Message}");
            }

            return fallback;
        }

        private static float GetTimelineX(Transform item)
        {
            var rectTransform = item as RectTransform;
            return rectTransform != null
                ? rectTransform.anchoredPosition.x
                : item.localPosition.x;
        }

        private static bool IsKingFacingSymbol(Transform king, Transform symbol)
        {
            bool kingFacesRight = king.localScale.x > 0f;
            bool symbolIsToTheRight = GetTimelineX(symbol) > GetTimelineX(king);
            return kingFacesRight == symbolIsToTheRight;
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

        protected override void ExecuteAction()
        {
            if (texts.Count == 0) return;

            // Information entries are repeatable. Only the final action label
            // advances the sequence.
            if (currentIndex != texts.Count - 1)
            {
                AnnounceCurrentText();
                return;
            }

            try
            {
                TolkWrapper.Speak(Localization.Get("advancing"), interrupt: true);

                // Chronology/new-king screens are driven by InputAct callbacks,
                // not by a Unity UI Button. TapAction invokes the action currently
                // registered by ChronoAct (normally StartGame at the final stage).
                if (InputAct.diff != null)
                {
                    Plugin.Logger.LogInfo("[ChronoScreen] Activating the current game action");
                    InputAct.diff.TapAction();
                    return;
                }

                var chronoAct = chronoTransform?.GetComponent<ChronoAct>()
                    ?? chronoTransform?.GetComponentInChildren<ChronoAct>(true);
                if (chronoAct != null)
                {
                    Plugin.Logger.LogWarning("[ChronoScreen] InputAct unavailable; using StartGame fallback");
                    chronoAct.StartGame();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[ChronoScreen] Could not advance: {ex}");
                TolkWrapper.Speak(Localization.Get("action_failed"), interrupt: true);
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
