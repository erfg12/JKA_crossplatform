using System.Net;
using System.Net.Sockets;

namespace JkaProtocolProxy;

public class ProxyService
{
    public string ServerIp { get; set; } = "135.125.145.49";
    public int ServerPort { get; set; } = 29070;

    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public event Action<string>? OnLog;

    private EventTextWriter? _consoleWriter;

    public void Start()
    {
        if (_cts != null) return;

        Program.PCGameServerIp = ServerIp;
        Program.PCGameServerPort = ServerPort;

        // Redirect Console.WriteLine to our event
        _consoleWriter = new EventTextWriter();
        _consoleWriter.OnWrite += message => OnLog?.Invoke(message);
        Console.SetOut(_consoleWriter);

        _cts = new CancellationTokenSource();
        _runTask = Task.Run(() => RunAllAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();
        if (_runTask != null)
            await _runTask.ConfigureAwait(false);
        _cts = null;
        _runTask = null;

        // Restore console output
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        _consoleWriter = null;
    }

    public string GetLocalIP()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("8.8.8.8", 80);
        return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
    }

    private async Task RunAllAsync(CancellationToken ct)
    {
        FirewallManager.OpenPorts();

        var myIp = GetLocalIP();
        Log($"[INIT] Starting... UDP Redirect To: {myIp}:29070");

        await Task.WhenAll(
            RunForeverAsync("Game Proxy", () => RunGameProxyAsync(ct), ct),
            RunForeverAsync("HTTP Health", () => MatchmakingRedirector.StartHealthListener(80), ct),
            RunForeverAsync("UDP Matchmaking", () => MatchmakingRedirector.StartUdpListenerAsync(30000, myIp, 29070), ct)
        );
    }

    private async Task RunForeverAsync(string name, Func<Task> serviceFactory, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Log($"[{name}] Starting...");
                await serviceFactory();
                Log($"[WARN] [{name}] Service exited unexpectedly - restarting in 3s...");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log($"[CRITICAL] [{name}] Crashed: {ex.Message} - restarting in 3s...");
            }

            try { await Task.Delay(3000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunGameProxyAsync(CancellationToken ct)
    {
        var proxyEngine = new JkaProxyEngine();
        await proxyEngine.RunProxyLoopAsync(ct);
    }

    private void Log(string message) => OnLog?.Invoke(message);
}