using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JKAServerBrowser;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JkaProtocolProxy.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ServerInfo> _servers = new();

    [ObservableProperty]
    private ServerInfo? _selectedServer;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _detailsText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logItems = new();

    [ObservableProperty]
    private LogEntry? _selectedLogItem;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty] 
    private string _serverIpText = string.Empty;

    [ObservableProperty] 
    private string _serverPortText = string.Empty;

    private readonly ProxyService _proxy = new();

    public bool IsPortValid => int.TryParse(ServerPortText, out var p) && p >= 1 && p <= 65535;

    partial void OnServerPortTextChanged(string value) => OnPropertyChanged(nameof(IsPortValid));

    partial void OnSelectedServerChanged(ServerInfo? value)
    {
        // Called automatically by CommunityToolkit whenever selection changes
        if (value != null)
        {
            ServerIpText = value.Address;
            ServerPortText = value.Port.ToString();
            DetailsText = value.ToString();
        }
    }

    public string GridTooltip => !IsRunning
    ? "Double-click a server to connect"
    : "Stop proxy to browse servers";

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(GridTooltip));
    }

    [RelayCommand]
    public void StartProxy()
    {
        if (string.IsNullOrWhiteSpace(ServerIpText))
        {
            Console.WriteLine("Cannot start proxy: IP is empty.");
            return;
        }

        if (!int.TryParse(ServerPortText, out var port) || port < 1 || port > 65535)
        {
            Console.WriteLine($"Cannot start proxy: '{ServerPortText}' is not a valid port (1–65535).");
            return;
        }

        _proxy.ServerIp = ServerIpText;
        _proxy.ServerPort = int.TryParse(ServerPortText, out var p) ? p : 0;

        // Unsubscribe first to prevent duplicate handlers
        _proxy.OnLog -= OnProxyLog;
        _proxy.OnLog += OnProxyLog;

        _proxy.Start();
        Console.WriteLine($"Sending game client to server: {ServerIpText}:{ServerPortText}");
        IsRunning = true;
    }

    private void OnProxyLog(string message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LogItems.Add(new LogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                Level = "INFO",
                Message = message
            });
        });
    }

    [RelayCommand]
    public async void StopProxy()
    {
        _proxy.StopAsync();
        _proxy.OnLog -= OnProxyLog;
        IsRunning = false;
        Console.WriteLine("proxy server stopped.");
    }

    [RelayCommand]
    private async Task RefreshServersAsync()
    {
        IsLoading = true;
        Servers.Clear();

        using var master = new JKAMasterClient();
        var (host, port) = JKAMasterClient.MasterServers[0];
        var serverList = await master.GetServerListAsync(host, port);

        await master.QueryAllServersAsync(serverList, onResult: info =>
        {
            // Must dispatch to UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Servers.Add(info));
        });

        IsLoading = false;
    }
}