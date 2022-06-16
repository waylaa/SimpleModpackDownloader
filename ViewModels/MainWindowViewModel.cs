using ForgedCurse;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SimpleModpackDownloader.Models;
using SimpleModpackDownloader.Native;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl.Http;

namespace SimpleModpackDownloader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive]
    public string TitlebarName { get; set; } = "Simple Modpack Downloader";

    [Reactive]
    public string ManifestFilePath { get; set; }

    [Reactive]
    public string ImportationFolderPath { get; set; }

    [Reactive]
    public bool HasInternetConnection { get; set; }

    [ObservableAsProperty]
    public bool IsDownloading { get; set; }

    public ReactiveCommand<Unit, string> OpenManifestFileDialog { get; }

    public ReactiveCommand<Unit, string> OpenImportationFolderDialog { get; }

    public ReactiveCommand<Unit, Unit> StartDownloadAndImportation { get; }

    public ReactiveCommand<Unit, Unit> OpenGithubRepository { get; }

    public MainWindowViewModel()
    {
        this.WhenAnyValue(x => x.HasInternetConnection)
            .Subscribe(x => HasInternetConnection = NativeMethods.IsConnectedToInternet());

        IObservable<bool> canStartDownloadAndImportation = this.WhenAnyValue(x => x.ManifestFilePath, x => x.ImportationFolderPath,
                                                                             selector: (manifestFilePath, importationFolderPath) => !string.IsNullOrWhiteSpace(manifestFilePath) &&
                                                                                                                                    !string.IsNullOrWhiteSpace(importationFolderPath));

        OpenImportationFolderDialog = ReactiveCommand.Create(OpenImportationFolderDialogImpl);
        OpenImportationFolderDialog.ThrownExceptions.Subscribe(ex => Log.Error(ex, ex.Message));
        OpenImportationFolderDialog.Subscribe(newImportationFolderPathValue => ImportationFolderPath = newImportationFolderPathValue);

        OpenManifestFileDialog = ReactiveCommand.Create(OpenManifestFileDialogImpl);
        OpenManifestFileDialog.Subscribe(value => ManifestFilePath = value);
        OpenManifestFileDialog.ThrownExceptions.Subscribe(ex =>
        {
            TitlebarName = "Simple Modpack Downloader";
            Log.Error(ex, ex.Message);
        });

        StartDownloadAndImportation = ReactiveCommand.CreateFromTask(() => StartDownloadAndImportationAsyncImpl(), canStartDownloadAndImportation);
        StartDownloadAndImportation.IsExecuting.ToPropertyEx(this, x => x.IsDownloading);
        StartDownloadAndImportation.ThrownExceptions.Subscribe(ex =>
        {
            TitlebarName = "Simple Modpack Downloader";
            Log.Error(ex, ex.Message);
        });

        OpenGithubRepository = ReactiveCommand.Create(OpenGithubLinkImpl);
        OpenGithubRepository.ThrownExceptions.Subscribe(ex => Log.Error(ex, ex.Message));
    }

    public string OpenManifestFileDialogImpl()
    {
        OpenFileDialog manifestDialog = new()
        {
            Title = "Select a manifest.json file from a modpack",
            Filter = "Modpack Manifest Files|*.json",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
        };

        bool? result = manifestDialog.ShowDialog();

        if (result == null || result == false)
        {
            return string.Empty;
        }

        if (!manifestDialog.FileName.Contains("manifest.json"))
        {
            Log.Error("Select a file with a valid name.");
            return string.Empty;
        }

        return manifestDialog.FileName;
    }

    public string OpenImportationFolderDialogImpl()
    {
        VistaFolderBrowserDialog importationFolderDialog = new()
        {
            RootFolder = Environment.SpecialFolder.ApplicationData,
            Description = "Select a folder where the files will be imported",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            Multiselect = false,
        };

        bool? result = importationFolderDialog.ShowDialog();

        if (!result.HasValue || result == false)
        {
            return string.Empty;
        }

        // If the specified importation directory does not contain forge or fabric, create a new directory where the files will be stored there.
        return !new string[] { "Forge", "forge", "Fabric", "fabric" }.Any(importationFolderDialog.SelectedPath.Contains)
               ? Directory.CreateDirectory(Path.GetFullPath($"({DateTime.Now:dd.MM.yyyy}) {ManifestFilePath.Split('\\').Reverse().Skip(1).First()} Files", importationFolderDialog.SelectedPath)).FullName
               : importationFolderDialog.SelectedPath;
    }

    public async Task StartDownloadAndImportationAsyncImpl()
    {
        // Deserialize the manifest file and start downloading and importing files.
        await using FileStream deserializationStream = new(ManifestFilePath, FileMode.Open, FileAccess.Read, FileShare.None, default, true);
        Manifest manifest = await JsonSerializer.DeserializeAsync<Manifest>(deserializationStream);

        TitlebarName = $"SimpleModpackDownloader - Downloading: {manifest.Name}";

        using ForgeClient curseForge = new();
        await Parallel.ForEachAsync(manifest.Files, async (file, token) =>
        {
            string downloadUrl = await curseForge.Files.RetrieveDownloadUrl(file.ProjectID, file.FileID);

            string destination = Path.GetFileName(downloadUrl).EndsWith(".jar") ? Path.GetFullPath("mods", ImportationFolderPath)
                                                                                : Path.GetFullPath("resourcepacks", ImportationFolderPath);

            Log.Information($"Downloaded {Path.GetFileName(await downloadUrl.DownloadFileAsync(destination, cancellationToken: token))}");
        });

        // Copy overrides folder.
        CopyAll(new(Path.GetFullPath("overrides", Path.GetDirectoryName(ManifestFilePath))), new(ImportationFolderPath));

        Log.Information("Finished downloading.");
        TitlebarName = "Simple Modpack Downloader";
    }

    public void OpenGithubLinkImpl()
        => Process.Start(new ProcessStartInfo("cmd", $"/c start {"https://github.com/Whatareyoulaughingat/SimpleModpackDownloader".Replace("&", "^&")}") { CreateNoWindow = true })?.Dispose();

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}