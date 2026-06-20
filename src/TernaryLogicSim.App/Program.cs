using Avalonia;

namespace TernaryLogicSim.App
{
    internal static class Program
    {
        // Ponto de entrada. NÃO use código Avalonia antes de AppMain ser chamado.
        [System.STAThread]
        public static void Main(string[] args) =>
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
