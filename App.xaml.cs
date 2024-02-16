using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SimpleModpackDownloader.Services;
using System;
using System.IO;
using System.Windows;

namespace SimpleModpackDownloader;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    internal static string BaseDirectory => Path.GetFullPath("SimpleModpackDownloader", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    internal static string TemporaryDirectory => Path.GetFullPath("Temporary Files", BaseDirectory);

    internal static string LogsDirectory => Path.GetFullPath("Logs", BaseDirectory);

    protected override void OnStartup(StartupEventArgs args)
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(TemporaryDirectory);

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
