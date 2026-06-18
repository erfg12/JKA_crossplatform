using Avalonia.Controls;
using JkaProtocolProxy.UI.ViewModels;
using JKAServerBrowser;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace JkaProtocolProxy.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private bool _sortAscending = true;
    private string? _lastSortedHeader;

    private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        var header = e.Column.Header?.ToString();

        if (_lastSortedHeader == header)
            _sortAscending = !_sortAscending;
        else
        {
            _sortAscending = true;
            _lastSortedHeader = header;
        }

        Func<ServerInfo, object> keySelector = header switch
        {
            "Host" => s => s.HostName,
            "Map" => s => s.MapName,
            "Players" => s => (object)s.PlayerCount,
            "Max" => s => (object)s.MaxClients,
            "Game Type" => s => s.GameType,
            "Mod" => s => s.Mod,
            "Version" => s => s.Version,
            _ => s => s.HostName
        };

        var vm = (MainViewModel)DataContext!;
        var sorted = _sortAscending
            ? vm.Servers.OrderBy(keySelector)
            : vm.Servers.OrderByDescending(keySelector);

        vm.Servers = new ObservableCollection<ServerInfo>(sorted);

        e.Handled = true;
    }
}
