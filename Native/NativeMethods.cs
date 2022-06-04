using System.Runtime.InteropServices;

namespace SimpleModpackDownloader.Native;

public static class NativeMethods
{
    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);

    public static bool IsConnectedToInternet()
    {
        return InternetGetConnectedState(out _, 0);
    }
}
