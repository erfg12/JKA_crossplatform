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
}