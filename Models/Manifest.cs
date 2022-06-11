using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleModpackDownloader.Models;

public record File(
    [property: JsonPropertyName("projectID")] int ProjectID,
    [property: JsonPropertyName("fileID")] int FileID
);

public record Manifest(
    [property: JsonPropertyName("files")] IReadOnlyList<File> Files,
    [property: JsonPropertyName("name")] string Name
);
