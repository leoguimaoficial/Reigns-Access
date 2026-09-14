namespace ReignsAccess.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, args) => ShowFatalError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ShowFatalError(args.ExceptionObject as Exception ?? new Exception("Unknown error."));

        Application.Run(new InstallerForm());
    }

    private static void ShowFatalError(Exception exception)
    {
        MessageBox.Show(
            $"The installer encountered an unexpected error.\n\n{exception.Message}",
            "Reigns Access Installer",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
