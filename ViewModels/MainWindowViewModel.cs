using Downloader;
using ForgedCurse;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using SimpleModpackDownloader.Global;
using SimpleModpackDownloader.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SimpleModpackDownloader.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
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
        OpenManifestFileDialog.ThrownExceptions.Subscribe(ex => Log.Error(ex, ex.Message));
        OpenManifestFileDialog.Subscribe(newManifestFilePathValue => ManifestFilePath = newManifestFilePathValue);

        StartDownloadAndImportation = ReactiveCommand.CreateFromTask(() => StartDownloadAndImportationAsyncImpl(), canStartDownloadAndImportation);
        StartDownloadAndImportation.ThrownExceptions.Subscribe(ex => Log.Error(ex, ex.Message));
        StartDownloadAndImportation.IsExecuting.ToPropertyEx(this, x => x.IsDownloading);

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

        Log.Information("Selected a manifest.json file sucessfully.");
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

        Log.Information("Selected an importation path successfuly.");

        // If the specified importation directory does not contain forge or fabric, create a new directory where the files will be stored there.
        return !importationFolderDialog.SelectedPath.Contains("Forge") && !importationFolderDialog.SelectedPath.Contains("Fabric")
               ? Directory.CreateDirectory(Path.GetFullPath($"Files ({DateTime.Now:dd.MM.yyyy})", importationFolderDialog.SelectedPath)).FullName
               : importationFolderDialog.SelectedPath;
    }

    public async Task StartDownloadAndImportationAsyncImpl()
    {
        // Copy overrides folder.
        DirectoryInfo importationDirectoryInfo = new(ImportationFolderPath);

        string overridesFolderPath = Path.GetFullPath("overrides", Path.GetDirectoryName(ManifestFilePath));
        DirectoryInfo overridesDirectoryInfo = new(overridesFolderPath);

        CopyAll(overridesDirectoryInfo, importationDirectoryInfo);

        // Download mods and resource packs.
        string manifestJson = await File.ReadAllTextAsync(ManifestFilePath);
        dynamic manifest = JsonNode.Parse(manifestJson);

        using ForgeClient curseForge = new();

        await Parallel.ForEachAsync((JsonArray)manifest["files"], async (node, token) =>
        {
            string downloadUrl = await curseForge.Files.RetrieveDownloadUrl((int)node["projectID"], (int)node["fileID"]);
            string fileName = Path.GetFileName(new Uri(downloadUrl).AbsolutePath);

            string destination = fileName.EndsWith(".jar") ? Path.GetFullPath(fileName, Path.GetFullPath("mods", ImportationFolderPath))
                                                           : Path.GetFullPath(fileName, Path.GetFullPath("resourcepacks", ImportationFolderPath));

            DownloadService downloader = await DownloadFileAsync(downloadUrl, fileName, destination).ConfigureAwait(false);
            downloader.Clear();
        });

        Log.Information("Finished downloading.");
    }

    public void OpenGithubLinkImpl()
        => Process.Start(new ProcessStartInfo("cmd", $"/c start {"https://github.com/Whatareyoulaughingat/SimpleModpackDownloader".Replace("&", "^&")}") { CreateNoWindow = true })?.Dispose();

    private async Task<DownloadService> DownloadFileAsync(string downloadUrl, string fileName, string destination)
    {
        DownloadService perFileDownloader = new(new DownloadConfiguration
        {
            TempDirectory = Paths.TemporaryDirectory,
            MaxTryAgainOnFailover = 2,
            OnTheFlyDownload = false,
            ParallelDownload = true,
            BufferBlockSize = 8192,
            Timeout = 20000,
            ChunkCount = 1,
            RequestConfiguration =
            {
                Accept = "*/*",
                KeepAlive = true,
                ProtocolVersion = HttpVersion.Version11,
                UseDefaultCredentials = false,
                UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/6.0;)",
            }
        });

        Observable.FromEventPattern<AsyncCompletedEventArgs>(
            handler => perFileDownloader.DownloadFileCompleted += handler,
            handler => perFileDownloader.DownloadFileCompleted -= handler)
        .Subscribe(completed =>
        {
            if (completed.EventArgs.Cancelled)
            {
                Log.Information($"Canceled the download process.");
                return;
            }

            if (completed.EventArgs.Error != null)
            {
                Log.Error($"An error has occured while downloading a file. Error message: {completed.EventArgs.Error.Message}");
                return;
            }

            Log.Information($"Downloaded {fileName}");
        });

        await perFileDownloader.DownloadFileTaskAsync(downloadUrl, destination).ConfigureAwait(false);
        return perFileDownloader;
    }

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