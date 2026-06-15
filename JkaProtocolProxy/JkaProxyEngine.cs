using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JkaProtocolProxy.Networking;
using JkaProtocolProxy.OpenJk;

public class JkaProxyEngine
{
    private readonly IPEndPoint _remoteServerEndpoint;
    private readonly UdpClient _proxyListener;

    // Thread-safe atomic storage for the dynamic Console Endpoint
    private IPEndPoint? _gameConsoleClientEndpoint;
    private readonly object _endpointLock = new();

    public JkaProxyEngine()
    {
        _remoteServerEndpoint = new IPEndPoint(IPAddress.Parse(Program.PCGameServerIp), Program.PCGameServerPort);

        // Bind to all local interfaces (0.0.0.0) on the designated game port
        _proxyListener = new UdpClient(Program.PCGameServerPort);
    }

    public async Task RunProxyLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Receive incoming UDP datagram packets from any source
                UdpReceiveResult receiveResult = await _proxyListener.ReceiveAsync(cancellationToken);
                byte[] packetBuffer = receiveResult.Buffer;
                IPEndPoint senderEndpoint = receiveResult.RemoteEndPoint;

                if (!senderEndpoint.Address.Equals(_remoteServerEndpoint.Address))
                {
                    // --- DIRECTION A: TRAFFIC FROM GAME CONSOLE -> GOING TO SERVER ---
                    lock (_endpointLock) 
                    {
                        _gameConsoleClientEndpoint = senderEndpoint;
                    }

                    // Process modification in a worker pool thread context
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
                    // --- DIRECTION B: TRAFFIC FROM SERVER -> GOING BACK TO GAME CONSOLE ---
                    IPEndPoint? targetConsole;
                    lock (_endpointLock)
                    {
                        targetConsole = _gameConsoleClientEndpoint;
                    }

                    if (targetConsole != null)
                    {
                        // Fire-and-forget sending back to console so receiver loop isn't delayed
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _proxyListener.SendAsync(packetBuffer, packetBuffer.Length, targetConsole);
                            }
                            catch (Exception ex)
                            {
                                LogNetworkError("Pipeline B (Server->Console) Forwarding Error", ex.Message);
                            }
                        }, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break; // Graceful shutdown requested
            }
            catch (Exception ex)
            {
                LogNetworkError("Core Proxy Socket Receiver Loop Error", ex.Message);
            }
        }
    }

    private static byte[] ModifyPacketInline(byte[] packet)
    {
        // 1. OOB Validation Guard
        if (packet.Length < 5 || packet[0] != 0xFF || packet[1] != 0xFF || packet[2] != 0xFF || packet[3] != 0xFF)
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
            // Thread-safe console log isolating the hex-dump payload 
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[Observe] Intercepted Connect Request From Client:");
                Helper.PrintHexDump(packet);
                Console.ResetColor();
            }

            byte[] payloadArray = Array.Empty<byte>();
            byte[] headerArray = Array.Empty<byte>();
            byte[] pattern = { 0xff, 0xff, 0xff, 0xff };

            int splitIndex = Helper.FindPattern(packet, pattern);
            if (splitIndex != -1)
            {
                headerArray = new byte[splitIndex];
                Array.Copy(packet, 0, headerArray, 0, splitIndex);

                payloadArray = new byte[packet.Length - splitIndex];
                Array.Copy(packet, splitIndex, payloadArray, 0, packet.Length - splitIndex);
            }

            // Invoke Huffman modifications safely via bridge abstraction
            byte[] decodedPacket = OpenJKNetworkBridge.ModifyAndReconstructPacket(payloadArray);
            return headerArray.Concat(decodedPacket).ToArray();
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