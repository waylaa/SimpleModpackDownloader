using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Serilog;
using SimpleModpackDownloader.Infrastructure;
using SimpleModpackDownloader.Models;
using SimpleModpackDownloader.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleModpackDownloader.ViewModels;

public partial class MainViewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _manifestFilePath = string.Empty;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenManifestFileDialogCommand), nameof(OpenOutputDirectoryDialogCommand), nameof(DownloadCommand))]
    private bool _isNotDownloading = true;

    private readonly ReadOnlyCollection<string> _modLoaders = new List<string>() { "Forge", "forge", "Fabric", "fabric" }.AsReadOnly();

    [RelayCommand(CanExecute = nameof(IsNotDownloading))]
    private Task<string> OpenManifestFileDialog()
    {
        OpenFileDialog manifestDialog = new()
        {
            Title = "Select a manifest.json file from a modpack",
            Filter = "Modpack Manifest Files|*manifest.json",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
        };

        bool? result = manifestDialog.ShowDialog();

        if (result is null or false)
        {
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(manifestDialog.FileName);
    }

    [RelayCommand(CanExecute = nameof(IsNotDownloading))]
    private Task<string> OpenOutputDirectoryDialog()
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

        if (result is null or false)
        {
            return Task.FromResult(string.Empty);
        }

        // If the specified importation directory does not contain forge or fabric, create a new directory where the files will be stored there.
        return !_modLoaders.Any(importationFolderDialog.SelectedPath.Contains)
               ? Task.FromResult(Directory.CreateDirectory(Path.GetFullPath($"({DateTime.Now:dd.MM.yyyy}) {ManifestFilePath.Split('\\').Reverse().Skip(1).First()} Files", importationFolderDialog.SelectedPath)).FullName)
               : Task.FromResult(importationFolderDialog.SelectedPath);
    }

    [RelayCommand(CanExecute = nameof(IsNotDownloading))]
    public async Task DownloadAsync()
    {
        IsNotDownloading = false;

        await using FileStream deserializationStream = System.IO.File.OpenRead(ManifestFilePath);
        Manifest? manifest = await JsonSerializer.DeserializeAsync<Manifest>(deserializationStream);

        if (manifest is null)
        {
            Log.Fatal("Failed to deserialize manifest");
            return;
        }

        IDownloadsProvider provider = Ioc.Default.GetRequiredService<IDownloadsProvider>();

        using SemaphoreSlim limiter = new(initialCount: 2);

        ReadOnlyCollection<Task> downloads = manifest.Files.Select(async file =>
        {
            await limiter.WaitAsync();
            Result<string> result = await provider.DownloadAsync(file.ProjectID, file.FileID, OutputDirectory);

            if (!result.IsSucessful || result.Value is null)
            {
                Log.Error("Failed to download a mod");

                limiter.Release();
                return;
            }

            Log.Information($"Downloaded {Path.GetFileName(result.Value)}");
            limiter.Release();

        }).ToList().AsReadOnly();

        await Task.WhenAll(downloads);

        CopyRecursively
        (
            new DirectoryInfo(Path.GetFullPath("overrides", Path.GetDirectoryName(ManifestFilePath)!)),
            new DirectoryInfo(OutputDirectory)
        );

        Log.Information("Finished downloading.");
        IsNotDownloading = true;
    }

    [RelayCommand]
    public void OpenRepository()
        => Process.Start(new ProcessStartInfo("cmd", $"/c start {"https://github.com/waylaa/SimpleModpackDownloader".Replace("&", "^&")}") { CreateNoWindow = true })?.Dispose();

    private static void CopyRecursively(DirectoryInfo source, DirectoryInfo output)
    {
        Directory.CreateDirectory(output.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(output.FullName, file.Name), true);
        }

        // Copy each subdirectory.
        foreach (DirectoryInfo subdirectory in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = output.CreateSubdirectory(subdirectory.Name);
            CopyRecursively(subdirectory, nextTargetSubDir);
        }
    }
}
