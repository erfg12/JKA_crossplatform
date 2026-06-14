using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using JkaProtocolProxy.Networking;
using JkaProtocolProxy.OpenJk;

const int GamePort = 29070;

// Target the actual WAN/Internet IP of the remote game server you observed in Wireshark
const string RemoteGameServerIp = "135.148.31.103";
IPEndPoint remoteServerEndpoint = new IPEndPoint(IPAddress.Parse(RemoteGameServerIp), GamePort);

// Bind a standard socket to GamePort on ALL local interfaces (0.0.0.0), catching the Hotspot stream
using var proxyListener = new UdpClient(GamePort);

Console.WriteLine("=============================================================");
Console.WriteLine($"[PROXY ACTIVE] Listening on UDP port {GamePort} via Hotspot...");
Console.WriteLine($"Target Server: {RemoteGameServerIp}:{GamePort}");
Console.WriteLine("=============================================================");

// Tracks the Game Console's dynamic local endpoint so we know where to route server replies
IPEndPoint? gameConsoleClientEndpoint = null;

while (true)
{
    try
    {
        // Receive raw data from the socket interface
        UdpReceiveResult receiveResult = await proxyListener.ReceiveAsync();
        byte[] packetBuffer = receiveResult.Buffer;
        IPEndPoint senderEndpoint = receiveResult.RemoteEndPoint;

        // Route traffic depending on the source origin
        if (!senderEndpoint.Address.Equals(remoteServerEndpoint.Address))
        {
            // --- DIRECTION A: TRAFFIC FROM GAME CONSOLE -> GOING TO SERVER ---
            gameConsoleClientEndpoint = senderEndpoint; // Track/update the Game Console's current local socket address

            // Inspect and mutate the packet data if it matches our protocol rewrite constraints
            packetBuffer = ModifyPacketInline(packetBuffer);

            // Forward the final payload data out to the public game server
            await proxyListener.SendAsync(packetBuffer, packetBuffer.Length, remoteServerEndpoint);
        }
        else
        {
            // --- DIRECTION B: TRAFFIC FROM SERVER -> GOING BACK TO GAME CONSOLE ---
            if (gameConsoleClientEndpoint != null)
            {
                // Forward the server's response straight back to the game console link
                await proxyListener.SendAsync(packetBuffer, packetBuffer.Length, gameConsoleClientEndpoint);
            }
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error in Socket Loop]: {ex.Message}");
        Console.ResetColor();
    }
}

static byte[] ModifyPacketInline(byte[] packet)
{
    // 1. OOB Validation Guard
    // Total size must fit at least the 4-byte 0xFFFFFFFF header and some text data
    if (packet.Length < 5 || packet[0] != 0xFF || packet[1] != 0xFF || packet[2] != 0xFF || packet[3] != 0xFF)
    {
        return packet; // Standard binary game state traffic; pass through with zero allocations
    }

    // 2. Extract engine command string (Starts right after the 4-byte 0xFF prefix)
    ReadOnlySpan<byte> udpPayload = packet;
    if (!ConnectionlessPacket.TryGetCommandText(udpPayload, out string commandText))
    {
        return packet;
    }

    // Debug print for any incoming out-of-band commands
    if (commandText.StartsWith("connect", StringComparison.Ordinal))
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"[Observe] Intercepted Connect Request:");
        Helper.PrintHexDump(packet);

        /////////////// Send to OpenJK-console to parse huffman, change protocol, and put back on the wire
        // 1. split packet, header from encoded huffman parts.
        byte[] payloadArray = new byte[0];
        byte[] headerArray = new byte[0];

        byte[] pattern = { 0xff, 0xff, 0xff, 0xff };
        int splitIndex = Helper.FindPattern(packet, pattern);

        if (splitIndex != -1)
        {
            // header of UDP packet
            headerArray = new byte[splitIndex];
            Array.Copy(packet, 0, headerArray, 0, splitIndex);

            // actual huffman encoded packet
            payloadArray = new byte[packet.Length - splitIndex];
            Array.Copy(packet, splitIndex, payloadArray, 0, packet.Length - splitIndex);

            // Output for verification
            Console.WriteLine($"Header Length: {headerArray.Length}");
            Console.WriteLine($"Payload Length: {payloadArray.Length}");
        }
        // 2. send huffman part to OpenJK-console, response should be modified protocol and re-encoded
        byte[] decodedPacket = OpenJKNetworkBridge.ModifyAndReconstructPacket(payloadArray);
        packet = headerArray.Concat(decodedPacket).ToArray();

        Console.ResetColor();
        return packet;
    }

    return packet; // Command was OOB but didn't match handshake requirements; return unmodified
}