using JkaProtocolProxy.Networking;
using JkaProtocolProxy.OpenJk;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class JkaProxyEngine
{
    private readonly IPEndPoint _remoteServerEndpoint;
    private readonly UdpClient _proxyListener;

    private IPEndPoint? _gameConsoleClientEndpoint;
    private DateTime _lastConsoleActivity = DateTime.MinValue;
    private readonly object _endpointLock = new();

    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(30);

    public JkaProxyEngine()
    {
        _remoteServerEndpoint = new IPEndPoint(IPAddress.Parse(Program.PCGameServerIp), Program.PCGameServerPort);
        _proxyListener = new UdpClient(Program.PCGameServerPort);
    }

    public async Task RunProxyLoopAsync(CancellationToken cancellationToken)
    {
        // Background timeout watcher
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, cancellationToken);
                    lock (_endpointLock)
                    {
                        if (_gameConsoleClientEndpoint != null &&
                            DateTime.UtcNow - _lastConsoleActivity > ClientTimeout)
                        {
                            Console.WriteLine($"[Proxy] Console client {_gameConsoleClientEndpoint} timed out - clearing endpoint.");
                            _gameConsoleClientEndpoint = null;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult receiveResult = await _proxyListener.ReceiveAsync(cancellationToken);
                byte[] packetBuffer = receiveResult.Buffer;
                IPEndPoint senderEndpoint = receiveResult.RemoteEndPoint;

                if (!senderEndpoint.Address.Equals(_remoteServerEndpoint.Address))
                {
                    // --- DIRECTION A: CONSOLE -> SERVER ---
                    lock (_endpointLock)
                    {
                        if (_gameConsoleClientEndpoint == null ||
                            !_gameConsoleClientEndpoint.Equals(senderEndpoint))
                        {
                            Console.WriteLine($"[Proxy] Console client connected: {senderEndpoint}");
                            _gameConsoleClientEndpoint = senderEndpoint;
                        }
                        _lastConsoleActivity = DateTime.UtcNow;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            byte[] modifiedBuffer = ModifyPacketInline(packetBuffer);
                            await _proxyListener.SendAsync(modifiedBuffer, modifiedBuffer.Length, _remoteServerEndpoint);
                        }
                        catch (Exception ex)
                        {
                            LogNetworkError("Pipeline A (Console->Server) Forwarding Error", ex.Message);
                        }
                    }, cancellationToken);
                }
                else
                {
                    // --- DIRECTION B: SERVER -> CONSOLE ---
                    IPEndPoint? targetConsole;
                    lock (_endpointLock)
                    {
                        targetConsole = _gameConsoleClientEndpoint;
                    }

                    if (targetConsole != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _proxyListener.SendAsync(packetBuffer, packetBuffer.Length, targetConsole);
                            }
                            catch (SocketException ex)
                            {
                                LogNetworkError("Pipeline B - Console unreachable, clearing endpoint", ex.Message);
                                lock (_endpointLock)
                                {
                                    _gameConsoleClientEndpoint = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogNetworkError("Pipeline B (Server->Console) Forwarding Error", ex.Message);
                            }
                        }, cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine("[Proxy] Dropping server packet - no active console client.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                // Windows ICMP "port unreachable" bubbling up as a SocketException - safe to ignore
                // This happens when a UDP packet is sent to a port with no listener
                Console.WriteLine("[Proxy] Remote host rejected a packet (ICMP unreachable) - continuing...");
                continue;
            }
            catch (Exception ex)
            {
                LogNetworkError("Core Proxy Socket Receiver Loop Error", ex.Message);
            }
        }
    }

    public static byte[] SetUserinfo(byte[] packet, string userinfo)
    {
        int commandEnd = 4; // skip FF FF FF FF
        while (commandEnd < packet.Length && packet[commandEnd] != 0x20)
            commandEnd++;
        commandEnd++; // skip space after "connect"

        byte[] raw = System.Text.Encoding.Latin1.GetBytes(userinfo);
        byte[] compressed = JKHuffman.Compress(raw);

        byte[] result = new byte[commandEnd + compressed.Length];
        Array.Copy(packet, 0, result, 0, commandEnd);
        Array.Copy(compressed, 0, result, commandEnd, compressed.Length);
        return result;
    }

    private static byte[] ModifyPacketInline(byte[] packet)
    {
        if (packet.Length < 5 ||
            packet[0] != 0xFF || packet[1] != 0xFF ||
            packet[2] != 0xFF || packet[3] != 0xFF)
        {
            return packet;
        }

        ReadOnlySpan<byte> udpPayload = packet;
        if (!ConnectionlessPacket.TryGetCommandText(udpPayload, out string commandText))
        {
            return packet;
        }

        if (commandText.StartsWith("connect", StringComparison.Ordinal))
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[Observe] Intercepted Connect Request From Client:");
                Helper.PrintHexDump(packet);
                Console.ResetColor();
            }

            var decodedUserInfo = ConnectPacket.GetUserinfo(packet);
            Console.WriteLine($"[Before]: {decodedUserInfo}");

            string? modified = decodedUserInfo?.Replace("\\protocol\\4007", "\\protocol\\26");
            Console.WriteLine($"[After]:  {modified}");

            byte[] result = packet;
            if (!string.IsNullOrEmpty(modified))
                result = ConnectPacket.SetUserinfo(packet, modified);

            Helper.PrintHexDump(result);
            return result;
        }

        return packet;
    }

    private static void LogNetworkError(string context, string message)
    {
        lock (Console.Out)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{context}]: {message}");
            Console.ResetColor();
        }
    }
}