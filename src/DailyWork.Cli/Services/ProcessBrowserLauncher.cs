using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DailyWork.Cli;

public class ProcessBrowserLauncher : IBrowserLauncher
{
    public void Open(string url)
    {
        ProcessStartInfo startInfo = CreateStartInfo(url);
        using var process = Process.Start(startInfo);
    }

    internal static ProcessStartInfo CreateStartInfo(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new ProcessStartInfo("open", url);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new ProcessStartInfo("xdg-open", url);
        }

        return new ProcessStartInfo("cmd", $"/c start {url}")
        {
            CreateNoWindow = true,
        };
    }
}
