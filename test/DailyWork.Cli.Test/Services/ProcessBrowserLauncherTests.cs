using System.Runtime.InteropServices;

namespace DailyWork.Cli.Test;

public class ProcessBrowserLauncherTests
{
    [Fact]
    public void CreateStartInfo_ReturnsCorrectCommand_ForCurrentPlatform()
    {
        const string url = "https://localhost:7200";

        System.Diagnostics.ProcessStartInfo startInfo =
            ProcessBrowserLauncher.CreateStartInfo(url);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Equal("open", startInfo.FileName);
            Assert.Equal(url, startInfo.Arguments);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Equal("xdg-open", startInfo.FileName);
            Assert.Equal(url, startInfo.Arguments);
        }
        else
        {
            Assert.Equal("cmd", startInfo.FileName);
            Assert.Contains(url, startInfo.Arguments);
            Assert.True(startInfo.CreateNoWindow);
        }
    }
}
