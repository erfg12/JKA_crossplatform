using System.Buffers.Binary;
using System.Collections;
using System.Text;

namespace JkaProtocolProxy.OpenJk;

/// <summary>
/// OpenJK connectionless (OOB) UDP payloads used during client connect.
/// See codemp/qcommon/net_chan.cpp NET_OutOfBandData / NET_OutOfBandPrint and
/// codemp/client/cl_main.cpp CL_CheckForResend.
/// </summary>
internal static class ConnectionlessPacket
{
    public const int OobMarker = -1; // 0xFFFFFFFF

    public static bool IsConnectionless(ReadOnlySpan<byte> udpPayload) =>
        udpPayload.Length >= 4 && BinaryPrimitives.ReadInt32LittleEndian(udpPayload) == OobMarker;

    public static bool TryGetCommandText(ReadOnlySpan<byte> udpPayload, out string commandText)
    {
        commandText = string.Empty;
        if (!IsConnectionless(udpPayload))
        {
            return false;
        }

        commandText = Encoding.ASCII.GetString(udpPayload[4..]);
        return true;
    }
}
