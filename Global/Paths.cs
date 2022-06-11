using System;
using System.IO;

namespace SimpleModpackDownloader.Global;

public static class Paths
{
    public static string BaseDirectory => Path.GetFullPath("SimpleModpackDownloader", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string TemporaryDirectory => Path.GetFullPath("Temporary Files", BaseDirectory);

    public static string LogsDirectory => Path.GetFullPath("Logs", BaseDirectory);
}
