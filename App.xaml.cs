using darknet.wpf;
using SimpleModpackDownloader.Views;
using ReactiveUI;
using SimpleModpackDownloader.Global;
using Splat;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;

namespace SimpleModpackDownloader;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Directory.CreateDirectory(Paths.BaseDirectory);
        Directory.CreateDirectory(Paths.LogsDirectory);
        Directory.CreateDirectory(Paths.TemporaryDirectory);

        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());

        // Set a dark titlebar for this process.
        DarkNet.SetDarkModeAllowedForProcess(true);

        MainWindow mainWindow = new();
        Observable.FromEventPattern<EventHandler, EventArgs>(
            handler => mainWindow.SourceInitialized += handler,
            handler => mainWindow.SourceInitialized -= handler)
        .Subscribe(x => DarkNet.SetDarkModeAllowedForWindow(mainWindow, true));

        mainWindow.Show();
    }
}
