using Avalonia;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JkaProtocolProxy.UI.Desktop;

class Program
{
    // Native macOS system call to get the current User ID
    [DllImport("libc", EntryPoint = "getuid")]
    private static extern uint getuid();

    [STAThread]
    public static void Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (getuid() != 0)
            {
                try
                {
                    string? currentPath = Process.GetCurrentProcess().MainModule?.FileName;

                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        // Safely get the directory name, defaulting to current folder if null
                        string workingDir = System.IO.Path.GetDirectoryName(currentPath) ?? ".";

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "osascript",
                            // This string is fully escaped and scopes workingDir correctly
                            Arguments = $"-e \"do shell script \"\"export PATH=/usr/bin:/bin:/usr/sbin:/sbin && cd '{workingDir}' && '{currentPath}'\"\" with administrator privileges\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process.Start(startInfo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Elevation initiation failed: {ex.Message}");
                }

                return;
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
