using System.Runtime.InteropServices;

namespace SimpleModpackDownloader.Native;

public static class NativeMethods
{
    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
}
