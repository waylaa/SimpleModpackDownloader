using System;
using System.IO;

namespace SimpleModpackDownloader.Global;

internal static class Paths
{
    internal static string BaseDirectory => Path.GetFullPath("SimpleModpackDownloader", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    internal static string TemporaryDirectory => Path.GetFullPath("Temporary Files", BaseDirectory);

    internal static string LogsDirectory => Path.GetFullPath("Logs", BaseDirectory);
}
