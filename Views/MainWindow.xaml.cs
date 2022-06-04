using ReactiveUI;
using Serilog;
using SimpleModpackDownloader.Global;
using SimpleModpackDownloader.ViewModels;
using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;

namespace SimpleModpackDownloader.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();

        Log.Logger = new LoggerConfiguration()
           .WriteTo.Async(x => x.File(Path.GetFullPath("log.txt", Paths.LogsDirectory), rollingInterval: RollingInterval.Day))
           .WriteTo.Async(x => x.RichTextBox(Logger))
           .CreateLogger();

        this.WhenActivated(disposableRegistration =>
        {
            // Manifest file bindings.
            this.BindCommand(
                ViewModel,
                vm => vm.OpenManifestFileDialog,
                view => view.ManifestFileDialog)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.ManifestFileDialog.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.ManifestFileDialog.IsEnabled)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.ManifestFilePath,
                view => view.ManifestFilePath.Text)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.ManifestFilePath.IsEnabled)
            .DisposeWith(disposableRegistration);

            // Importation folder bindings.
            this.BindCommand(
                ViewModel,
                vm => vm.OpenImportationFolderDialog,
                view => view.ImportationFolderDialog)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.IsDownloading,
                view => view.ImportationFolderDialog.IsEnabled,
                value => !value)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.ImportationFolderDialog.IsEnabled)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.ImportationFolderPath,
                view => view.ImportationFolderPath.Text)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.ImportationFolderPath.IsEnabled)
            .DisposeWith(disposableRegistration);

            // File download bindings.
            this.BindCommand(
                ViewModel,
                vm => vm.StartDownloadAndImportation,
                view => view.StartDownload)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.StartDownload.IsEnabled)
            .DisposeWith(disposableRegistration);

            // Github repository bindings.
            this.BindCommand(
                ViewModel,
                vm => vm.OpenGithubRepository,
                view => view.GithubRepository)
            .DisposeWith(disposableRegistration);

            this.OneWayBind(
                ViewModel,
                vm => vm.HasInternetConnection,
                view => view.GithubRepository.IsEnabled)
            .DisposeWith(disposableRegistration);

            // Logger events.
            Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                handler => Logger.TextChanged += handler,
                handler => Logger.TextChanged -= handler)
            .Subscribe(x => Logger.ScrollToEnd());
        });
    }
}
