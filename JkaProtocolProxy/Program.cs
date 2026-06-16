using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    // ===================================================================
    //  CONFIGURATION VARIABLES
    // ===================================================================
    // Pick a server from https://serverlist.jkhub.org/#colGamename=1&colJK2MV=0&colVersion=0&displayFilters=0&autoRefresh=0&jk2version=1.04&jkaversion=1.01&sinversion=1.13&theme=dark&sortReverse=0&serverInfoMode=0&game=jka&sortBy=hostname&filterString=
    public static string PCGameServerIp = "135.125.145.49"; // game server IP address (can be changed to any PC server you want to join)
    public static int PCGameServerPort = 29070;             // game server port
    // ===================================================================

    public static string GetLocalIP()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("8.8.8.8", 80); // Doesn't actually send traffic
        return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
    }

    static async Task Main(string[] args)
    {
        FirewallManager.OpenPorts();

        // Catch anything that escapes to the top level - log it but never crash
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogCritical("UnhandledException", e.ExceptionObject?.ToString() ?? "Unknown");
        };
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogCritical("UnobservedTaskException", e.Exception?.ToString() ?? "Unknown");
            e.SetObserved(); // prevent process termination
        };

        // Intercept Ctrl+C - warn but keep running
        // Console.CancelKeyPress += (sender, e) =>
        // {
        //     e.Cancel = true; // do NOT exit
        //     LogWarning("Ctrl+C intercepted - proxy is keeping alive. Kill via Task Manager if needed.");
        // };

        var myIp = GetLocalIP();

        Console.WriteLine($"[INIT] Starting Multi-Protocol Redirector Engine...");
        Console.WriteLine($"[INIT] UDP Redirect To: {myIp}:29070");

        // Run all three services with auto-restart on failure
        await Task.WhenAll(
            RunForeverAsync("Game Proxy", RunGameProxyAsync),
            RunForeverAsync("HTTP Health", () => MatchmakingRedirector.StartHealthListener(80)),
            RunForeverAsync("UDP Matchmaking", () => MatchmakingRedirector.StartUdpListenerAsync(30000, myIp, 29070))
        );
    }

    /// <summary>
    /// Wraps a service loop so that if it throws or returns, it restarts after a short delay.
    /// This loop itself never exits.
    /// </summary>
    private static async Task RunForeverAsync(string name, Func<Task> serviceFactory)
    {
        while (true)
        {
            try
            {
                Console.WriteLine($"[{name}] Starting...");
                await serviceFactory();
                // If the task returns without throwing, restart it anyway
                LogWarning(name, "Service exited unexpectedly - restarting in 3s...");
            }
            catch (Exception ex)
            {
                LogCritical(name, $"Crashed: {ex.Message} - restarting in 3s...");
            }

            await Task.Delay(3000); // brief pause before restart
        }
    }

    private static async Task RunGameProxyAsync()
    {
        // Recreate the engine on each restart in case socket is in bad state
        var proxyEngine = new JkaProxyEngine();
        await proxyEngine.RunProxyLoopAsync(CancellationToken.None);
    }

    private static void LogWarning(string context, string message)
    {
        lock (Console.Out)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] [{context}]: {message}");
            Console.ResetColor();
        }
    }

    private static void LogWarning(string message)
    {
        lock (Console.Out)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN]: {message}");
            Console.ResetColor();
        }
    }

    private static void LogCritical(string context, string message)
    {
        lock (Console.Out)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CRITICAL] [{context}]: {message}");
            Console.ResetColor();
        }
    }
}