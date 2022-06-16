using ReactiveUI;
using SimpleModpackDownloader.Global;
using Splat;
using System.IO;
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
    }
}
