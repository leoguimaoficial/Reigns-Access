namespace ReignsAccess.Navigation.Screens
{
    /// <summary>
    /// Facade para o sistema de navegação de telas especiais.
    /// Mantém compatibilidade com o código existente.
    /// 
    /// A implementação real está em:
    /// - Screens/ScreenManager.cs (gerenciador)
    /// - Screens/ScreenNavigatorBase.cs (classe base)
    /// - Screens/DeathScreenNavigator.cs (tela de morte)
    /// </summary>
    public static class TitleScreenNavigator
    {
        /// <summary>
        /// Atualiza o sistema de telas especiais
        /// </summary>
        public static void Update()
        {
            ScreenManager.Update();
        }
        
        /// <summary>
        /// Verifica se alguma tela especial está ativa
        /// </summary>
        public static bool IsSpecialScreenActive()
        {
            return ScreenManager.IsAnyScreenActive();
        }
        
        /// <summary>
        /// Verifica se a tela de morte está ativa (compatibilidade)
        /// </summary>
        public static bool IsDeadKingScreenActive()
        {
            return DeathScreenNavigator.IsActive;
        }
        
        /// <summary>
        /// Navega para baixo
        /// </summary>
        public static void NavigateDown()
        {
            ScreenManager.NavigateDown();
        }
        
        /// <summary>
        /// Navega para cima
        /// </summary>
        public static void NavigateUp()
        {
            ScreenManager.NavigateUp();
        }
        
        /// <summary>
        /// Ativa/confirma (não usado atualmente)
        /// </summary>
        public static void Activate()
        {
            // Não faz nada
        }
        
        /// <summary>
        /// Repete o texto atual
        /// </summary>
        public static void RepeatCurrent()
        {
            ScreenManager.RepeatCurrent();
        }
        
        /// <summary>
        /// Fecha a tela especial ativa
        /// </summary>
        public static void CloseCurrentScreen()
        {
            ScreenManager.CloseCurrentScreen();
        }
    }
}
