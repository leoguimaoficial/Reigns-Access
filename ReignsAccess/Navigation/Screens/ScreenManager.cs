using System.Collections.Generic;
using ReignsAccess.Accessibility;

namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Gerenciador central de todas as telas especiais.
    /// Coordena os navegadores e garante que apenas um esteja ativo por vez.
    /// </summary>
    public static class ScreenManager
    {
        private static List<ScreenNavigatorBase> navigators = new List<ScreenNavigatorBase>();
        private static ScreenNavigatorBase activeNavigator = null;
        private static bool initialized = false;
        
        /// <summary>
        /// Inicializa todos os navegadores de tela
        /// </summary>
        public static void Initialize()
        {
            if (initialized) return;
            
            // Registrar todos os navegadores de tela
            // Sub-telas do menu Reino (devem ser verificadas primeiro!)
            navigators.Add(new MementoMoriNavigator());       // Memento Mori (galeria de mortes)
            navigators.Add(new ObjectivesScreenNavigator());  // Façanhas Reais
            navigators.Add(new BearersScreenNavigator());     // Galeria de Retratos
            
            // Telas principais
            navigators.Add(new KingDeathScreenNavigator());   // Tela "O Rei está morto"
            navigators.Add(new ChronoScreenNavigator());      // Cronologia após morte
            
            initialized = true;
        }
        
        /// <summary>
        /// Atualiza todos os navegadores e gerencia qual está ativo
        /// </summary>
        public static void Update()
        {
            if (!initialized)
            {
                Initialize();
            }
            
            // Verificar qual navegador deve estar ativo
            ScreenNavigatorBase newActive = null;
            
            foreach (var nav in navigators)
            {
                if (nav.IsScreenActive())
                {
                    newActive = nav;
                    break; // Primeiro que encontrar ativo, usa
                }
            }
            
            // Mudou de tela?
            if (newActive != activeNavigator)
            {
                activeNavigator = newActive;
            }
            
            // Atualizar apenas o navegador ativo
            activeNavigator?.Update();
        }
        
        /// <summary>
        /// Verifica se alguma tela especial está ativa
        /// </summary>
        public static bool IsAnyScreenActive()
        {
            return activeNavigator != null && activeNavigator.IsActive;
        }
        
        /// <summary>
        /// Retorna o navegador ativo atual
        /// </summary>
        public static ScreenNavigatorBase GetActiveNavigator()
        {
            return activeNavigator;
        }
        
        /// <summary>
        /// Navega para cima na tela ativa
        /// </summary>
        public static void NavigateUp()
        {
            activeNavigator?.NavigateUp();
        }
        
        /// <summary>
        /// Navega para baixo na tela ativa
        /// </summary>
        public static void NavigateDown()
        {
            activeNavigator?.NavigateDown();
        }
        
        /// <summary>
        /// Repete o texto atual
        /// </summary>
        public static void RepeatCurrent()
        {
            activeNavigator?.RepeatCurrent();
        }
        
        /// <summary>
        /// Fecha a tela ativa atual
        /// </summary>
        public static void CloseCurrentScreen()
        {
            activeNavigator?.CloseScreen();
        }
    }
}
