using System.Threading;

class Program
{
    // ===================================================================
    //  CONFIGURATION VARIABLES
    // ===================================================================
    // OpenJK-console target destination (where the proxy will forward packets to)
    public static string OpenJKDedIp = "192.168.0.10";      // should match system IP
    public static ushort OpenJKDedPort = 29070;             // this is the port to listen for

    // matchmaking server settings
    public static int MatchMakingServerPort = 30000;        // match making port to listen for incoming packets from the game console
    public static int MatchMakingServerHealthPort = 80;     // health server to ping back status 200

    // PC game server to join
    public static string PCGameServerIp = "135.125.145.49"; // game server IP address (can be changed to any PC server you want to join)
    public static int PCGameServerPort = 29070;
    // ===================================================================



    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        // 1. Initialize and run our newly updated thread-friendly proxy engine
        var proxyEngine = new JkaProxyEngine();

        Console.WriteLine($"[INIT] Starting Multi-Protocol Redirector Engine...");
        Console.WriteLine($"[INIT] UDP Redirect To: {OpenJKDedIp}:{OpenJKDedPort}");

        Task gameProxy = proxyEngine.RunProxyLoopAsync(cts.Token);
        Task httpTask = MatchmakingRedirector.StartHealthListener(MatchMakingServerHealthPort);
        Task udpTask = MatchmakingRedirector.StartUdpListenerAsync(MatchMakingServerPort, OpenJKDedIp, OpenJKDedPort);

        await Task.WhenAll(gameProxy, httpTask, udpTask);
    }
}