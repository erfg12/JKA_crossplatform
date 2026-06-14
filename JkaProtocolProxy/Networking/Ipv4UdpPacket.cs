using System.Buffers.Binary;
using System.Net.Sockets;

namespace JkaProtocolProxy.Networking;

internal static class Ipv4UdpPacket
{
    public static bool TryGetUdpPayload(ReadOnlySpan<byte> packet, int packetLength, out int udpPayloadOffset, out int udpPayloadLength)
    {
        udpPayloadOffset = 0;
        udpPayloadLength = 0;

        if (packetLength < 28)
        {
            return false;
        }

        int version = (packet[0] >> 4) & 0x0F;
        if (version != 4)
        {
            return false;
        }

        if (packet[9] != (byte)ProtocolType.Udp)
        {
            return false;
        }

        int ipHeaderLength = (packet[0] & 0x0F) * 4;
        if (ipHeaderLength < 20 || packetLength < ipHeaderLength + 8)
        {
            return false;
        }

        int udpOffset = ipHeaderLength;
        int udpLength = BinaryPrimitives.ReadUInt16BigEndian(packet[(udpOffset + 4)..]);
        if (udpLength < 8 || udpOffset + udpLength > packetLength)
        {
            udpPayloadOffset = udpOffset + 8;
            udpPayloadLength = packetLength - udpPayloadOffset;
            return udpPayloadLength > 0;
        }

        udpPayloadOffset = udpOffset + 8;
        udpPayloadLength = udpLength - 8;
        return true;
    }

    public static byte[] RebuildWithUdpPayload(ReadOnlySpan<byte> originalPacket, int originalLength, ReadOnlySpan<byte> newUdpPayload)
    {
        if (!TryGetUdpPayload(originalPacket, originalLength, out int _, out _))
        {
            throw new InvalidOperationException("Packet is not a valid IPv4 UDP datagram.");
        }

        int ipHeaderLength = (originalPacket[0] & 0x0F) * 4;
        int udpOffset = ipHeaderLength;
        int newPacketLength = ipHeaderLength + 8 + newUdpPayload.Length;
        var rebuilt = new byte[newPacketLength];

        originalPacket[..ipHeaderLength].CopyTo(rebuilt);
        originalPacket.Slice(udpOffset, 4).CopyTo(rebuilt.AsSpan(udpOffset));

        ushort udpLength = (ushort)(8 + newUdpPayload.Length);
        BinaryPrimitives.WriteUInt16BigEndian(rebuilt.AsSpan(udpOffset + 4), udpLength);
        BinaryPrimitives.WriteUInt16BigEndian(rebuilt.AsSpan(udpOffset + 6), 0);

        newUdpPayload.CopyTo(rebuilt.AsSpan(udpOffset + 8));

        BinaryPrimitives.WriteUInt16BigEndian(rebuilt.AsSpan(2), (ushort)newPacketLength);
        BinaryPrimitives.WriteUInt16BigEndian(rebuilt.AsSpan(10), 0);

        return rebuilt;
    }
}
