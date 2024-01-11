using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SimpleModpackDownloader.Models;

internal sealed record File(
    [property: JsonPropertyName("projectID")] int ProjectID,
    [property: JsonPropertyName("fileID")] int FileID
);

internal sealed record Manifest(
    [property: JsonPropertyName("files")] ReadOnlyCollection<File> Files,
    [property: JsonPropertyName("name")] string Name
);
