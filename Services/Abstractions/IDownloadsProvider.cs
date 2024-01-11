using SimpleModpackDownloader.Infrastructure;
using System.Threading.Tasks;

namespace SimpleModpackDownloader.Services.Abstractions;

internal interface IDownloadsProvider
{
    Task<Result<string>> DownloadAsync(int projectId, int fileId, string outputDirectory);
}
