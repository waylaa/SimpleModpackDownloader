using System;
using System.IO;

namespace SimpleModpackDownloader.Global;

public static class Paths
{
    public static string BaseDirectory => Path.GetFullPath("ModpackManifestDownloader", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string TemporaryDirectory => Path.GetFullPath("Temporary", BaseDirectory);

    public static string LogsDirectory => Path.GetFullPath("Logs", BaseDirectory);
}
