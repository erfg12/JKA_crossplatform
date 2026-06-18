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

    public void StopAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts = null;
        _runTask = null;
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

        var t1 = Task.Run(() => RunGameProxyAsync(ct), ct);
        var t2 = Task.Run(() => MatchmakingRedirector.StartHealthListener(80, ct), ct);
        var t3 = Task.Run(() => MatchmakingRedirector.StartUdpListenerAsync(30000, myIp, 29070, ct), ct);

        await Task.WhenAll(t1, t2, t3);
    }

    private async Task RunGameProxyAsync(CancellationToken ct)
    {
        var proxyEngine = new JkaProxyEngine();
        await proxyEngine.RunProxyLoopAsync(ct);
    }

    private void Log(string message) => OnLog?.Invoke(message);
}