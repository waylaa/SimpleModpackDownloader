using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SimpleModpackDownloader.Global;
using SimpleModpackDownloader.Services;
using System.IO;
using System.Windows;

namespace SimpleModpackDownloader;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs args)
    {
        Directory.CreateDirectory(Paths.BaseDirectory);
        Directory.CreateDirectory(Paths.LogsDirectory);
        Directory.CreateDirectory(Paths.TemporaryDirectory);

        Ioc.Default.ConfigureServices
        (
            new ServiceCollection()
            .AddHttpClient()
            .AddSingleton<CurseForgeService>()
            .BuildServiceProvider()
        );

        base.OnStartup(args);
    }
}
