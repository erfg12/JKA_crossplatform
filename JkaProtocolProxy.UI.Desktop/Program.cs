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
        // 1. Check if we are running on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // 2. Check if we are already running as root (UID 0)
            if (getuid() != 0)
            {
                try
                {
                    // 3. Find the path of this compiled binary inside the .app bundle
                    string currentPath = Process.GetCurrentProcess().MainModule?.FileName;

                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "osascript",
                            // Tells macOS to re-run this exact file context with administrator privileges
                            Arguments = $"-e \"do shell script \"\"export PATH=/usr/bin:/bin:/usr/sbin:/sbin && cd '{workingDir}' && '{currentPath}'\"\" with administrator privileges\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process.Start(startInfo);
                    }
                }
                catch (Exception ex)
                {
                    // Fallback log if elevation dialog is canceled or fails
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
