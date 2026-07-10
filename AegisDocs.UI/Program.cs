using System;
using Avalonia;

namespace AegisDocs.UI;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .With(new Win32PlatformOptions
            {
                CompositionMode = new[] { Win32CompositionMode.RedirectionSurface },
                RenderingMode = new[] { Win32RenderingMode.Software }
            });
}