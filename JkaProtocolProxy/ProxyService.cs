using System.Net;
using System.Net.Sockets;

namespace JkaProtocolProxy;

public class ProxyService
{
    private string _serverIp = string.Empty;
    public string ServerIp
    {
        get => _serverIp;
        set
        {
            _serverIp = value;
            UpdateEngineEndpoint();
        }
    }

    private int _serverPort;
    public int ServerPort
    {
        get => _serverPort;
        set
        {
            _serverPort = value;
            UpdateEngineEndpoint();
        }
    }

    private void UpdateEngineEndpoint()
    {
        if (_currentEngine != null && !string.IsNullOrEmpty(_serverIp) && _serverPort > 0)
        {
            _currentEngine.UpdateRemoteEndpoint(_serverIp, _serverPort);
        }
    }

    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private JkaProxyEngine? _currentEngine;
    private DNSProxyService? _dnsProxy;

    public event Action<string>? OnLog;

    private EventTextWriter? _consoleWriter;

    public void Start()
    {
        if (_cts != null) return;

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

        if (_currentEngine != null)
        {
            _currentEngine.Dispose();
            _currentEngine = null;
        }

        if (_runTask != null)
        {
            try
            {
                await _runTask;
            }
            catch (Exception ex)
            {
                // Task.WhenAll or individual tasks might throw when closed, which is fine
            }
            _runTask = null;
        }

        _cts.Dispose();
        _cts = null;

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
        try
        {
            FirewallManager.OpenPorts();
            var myIp = GetLocalIP();
            Log($"[INIT] Starting... UDP Redirect To: {myIp}:29070");

            _currentEngine = new JkaProxyEngine();
            _dnsProxy = new DNSProxyService();

            var t1 = Task.Run(() => RunGameProxyAsync(_currentEngine, ct), ct);
            var t2 = Task.Run(() => MatchmakingRedirector.StartHealthListener(80, ct), ct);
            var t3 = Task.Run(() => MatchmakingRedirector.StartUdpListenerAsync(30000, myIp, 29070, ct), ct);

            Task udpDNSTask = Task.Run(() => _dnsProxy.RunDNSUdpListener(myIp, ct));
            Task tcpDNSTask = Task.Run(() => _dnsProxy.RunDNSTcpListener(myIp, ct));

            await Task.WhenAll(udpDNSTask, tcpDNSTask, t1, t2, t3);
        }
        catch (Exception ex)
        {
            // When StopAsync closes the socket, Task.WhenAll will throw an exception.
            // Catching it here prevents your background worker from crashing silently.
            Log($"[INIT] Pipelines stopped: {ex.Message}");
        }
        finally
        {
            FirewallManager.ClosePorts();
            Log("[INIT] Firewall ports closed.");
        }
    }

    private async Task RunGameProxyAsync(JkaProxyEngine proxyEngine, CancellationToken ct)
    {
        await proxyEngine.RunProxyLoopAsync(ServerIp, ServerPort, ct);
    }

    private void Log(string message) => OnLog?.Invoke(message);
}