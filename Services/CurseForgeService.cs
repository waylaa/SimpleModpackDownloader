using CurseForge.APIClient;
using CurseForge.APIClient.Models;
using SimpleModpackDownloader.Infrastructure;
using SimpleModpackDownloader.Services.Abstractions;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleModpackDownloader.Services;

internal sealed class CurseForgeService : IDownloadsProvider
{
    private readonly HttpClient _client;

    private readonly string _apiKey;

    public CurseForgeService(IHttpClientFactory clientFactory, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(null, nameof(apiKey));
        }

        _apiKey = apiKey;
        _client = clientFactory.CreateClient();
    }

    public async Task<Result<string>> DownloadAsync(int projectId, int fileId, string outputDirectory)
    {
        try
        {
            using ApiClient client = new(_apiKey);
            GenericResponse<string> response = await client.GetModFileDownloadUrlAsync(projectId, fileId);

            if (string.IsNullOrWhiteSpace(response.Data))
            {
                return string.Empty;
            }

            string destination = Path.GetFileName(response.Data).EndsWith(".jar")
                ? Path.GetFullPath("mods", outputDirectory)
                : Path.GetFullPath("resourcepacks", outputDirectory);

            using Stream dataStream = await _client.GetStreamAsync(response.Data);
            using FileStream modFileStream = File.OpenRead(destination);

            await dataStream.CopyToAsync(modFileStream);
            return response.Data;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
