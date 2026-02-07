using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ReignsAccess.Accessibility;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Classe base para navegadores de telas especiais.
    /// Fornece funcionalidade comum de navegação por lista de textos.
    /// </summary>
    public abstract class ScreenNavigatorBase
    {
        protected List<string> texts = new List<string>();
        protected int currentIndex = 0;
        protected bool isActive = false;
        protected bool wasActive = false;
        protected string lastLanguage = "";
        
        protected float lastNavigationTime = 0f;
        protected readonly float navigationCooldown = 0.25f;
        
        public abstract string ScreenName { get; }
        public abstract bool IsScreenActive();
        protected abstract void CollectTexts();
        
        public virtual void Update()
        {
            bool currentlyActive = IsScreenActive();
            
            if (currentlyActive && !wasActive)
            {
                OnScreenEnter();
            }
            else if (!currentlyActive && wasActive)
            {
                OnScreenExit();
            }
            else if (currentlyActive && wasActive)
            {
                // Tela continua ativa - verificar se idioma mudou
                CheckLanguageChange();
            }
            
            wasActive = currentlyActive;
        }
        
        /// <summary>
        /// Verifica se o idioma mudou e re-coleta textos se necessário.
        /// </summary>
        protected virtual void CheckLanguageChange()
        {
            string currentLanguage = Core.Localization.CurrentLanguage;
            
            if (!string.IsNullOrEmpty(currentLanguage) && currentLanguage != lastLanguage)
            {
                lastLanguage = currentLanguage;
                
                // Re-coletar textos com novo idioma
                int previousIndex = currentIndex;
                CollectTexts();
                
                // Manter índice válido
                if (currentIndex >= texts.Count)
                {
                    currentIndex = texts.Count > 0 ? texts.Count - 1 : 0;
                }
                
                // Anunciar texto atual no novo idioma
                if (texts.Count > 0)
                {
                    AnnounceCurrentText();
                }
            }
        }
        
        protected virtual void OnScreenEnter()
        {
            isActive = true;
            lastLanguage = Core.Localization.CurrentLanguage;
            CollectTexts();
            currentIndex = 0;
            
            if (texts.Count > 0)
            {
                AnnounceCurrentText();
            }
        }
        
        protected virtual void OnScreenExit()
        {
            isActive = false;
            texts.Clear();
            currentIndex = 0;
        }
        
        public void NavigateUp()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                AnnounceCurrentText();
            }
        }
        
        public void NavigateDown()
        {
            if (currentIndex < texts.Count - 1)
            {
                currentIndex++;
                AnnounceCurrentText();
            }
        }
        
        public void NavigateToStart()
        {
            currentIndex = 0;
            AnnounceCurrentText();
        }
        
        public void NavigateToEnd()
        {
            currentIndex = texts.Count - 1;
            AnnounceCurrentText();
        }
        
        public void RepeatCurrent()
        {
            AnnounceCurrentText();
        }
        
        protected virtual void ExecuteAction()
        {
            if (texts.Count == 0) return;
            
            TolkWrapper.Speak(Core.Localization.Get("advancing"));
            SimulateActionClick();
        }
        
        protected void SimulateActionClick()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            
            var touch = canvas.transform.Find("touch");
            if (touch == null || !touch.gameObject.activeInHierarchy) return;
            
            var button = touch.GetComponent<Button>();
            if (button != null && button.enabled && button.interactable)
            {
                button.onClick.Invoke();
                return;
            }
            
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem);
                UnityEngine.EventSystems.ExecuteEvents.Execute(touch.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            }
        }
        
        protected virtual void AnnounceCurrentText()
        {
            if (currentIndex >= 0 && currentIndex < texts.Count)
            {
                string text = texts[currentIndex];
                string announceWithPosition = $"{text}. {currentIndex + 1}{Core.Localization.Get("position_of")}{texts.Count}";
                TolkWrapper.Speak(announceWithPosition);
            }
        }
        
        public bool IsActive => isActive;
        public int ItemCount => texts.Count;
        public int CurrentIndex => currentIndex;
        
        protected string GetText(Transform parent, string childName)
        {
            if (parent == null) return null;
            
            var child = parent.Find(childName);
            if (child == null || !child.gameObject.activeInHierarchy) return null;
            
            var textComp = child.GetComponent<Text>();
            if (textComp == null) return null;
            
            string text = textComp.text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        
        protected bool IsObjectActive(string path)
        {
            var obj = GameObject.Find(path);
            return obj != null && obj.activeInHierarchy;
        }
        
        protected Transform FindTransform(string path)
        {
            var obj = GameObject.Find(path);
            return obj?.transform;
        }
        
        protected void AddText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                texts.Add(text);
            }
        }
        
        /// <summary>
        /// Tenta fechar a tela atual (pode ser sobrescrito)
        /// </summary>
        public virtual void CloseScreen()
        {
            // Implementação padrão: não faz nada
            // Subclasses devem sobrescrever se tiverem botão de fechar
        }
    }
}
