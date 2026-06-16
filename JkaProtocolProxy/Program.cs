using System.Threading;

class Program
{
    // ===================================================================
    //  CONFIGURATION VARIABLES
    // ===================================================================
    // OpenJK-console target destination (where the proxy will forward packets to)
    public static string OpenJKDedIp = "192.168.0.10";      // should match this computer's IP
    public static ushort OpenJKDedPort = 29070;             // this is the port to listen for, should stay at 29070

    // matchmaking server settings
    public static int MatchMakingServerPort = 30000;        // match making port to listen for incoming packets from the game console
    public static int MatchMakingServerHealthPort = 80;     // health server to ping back status 200

    // PC game server to join
    public static string PCGameServerIp = "135.125.145.49"; // game server IP address (can be changed to any PC server you want to join)
    public static int PCGameServerPort = 29070;             // game server port
    // ===================================================================

    static async Task Main(string[] args)
    {
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
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // do NOT exit
            LogWarning("Ctrl+C intercepted - proxy is keeping alive. Kill via Task Manager if needed.");
        };

        Console.WriteLine($"[INIT] Starting Multi-Protocol Redirector Engine...");
        Console.WriteLine($"[INIT] UDP Redirect To: {OpenJKDedIp}:{OpenJKDedPort}");

        // Run all three services with auto-restart on failure
        await Task.WhenAll(
            RunForeverAsync("Game Proxy", RunGameProxyAsync),
            RunForeverAsync("HTTP Health", () => MatchmakingRedirector.StartHealthListener(MatchMakingServerHealthPort)),
            RunForeverAsync("UDP Matchmaking", () => MatchmakingRedirector.StartUdpListenerAsync(MatchMakingServerPort, OpenJKDedIp, OpenJKDedPort))
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