public static class ConnectPacket
{
    /// <summary>
    /// Parses a JK/Q3 OOB connect packet and returns the raw userinfo string.
    /// Returns null if the packet is not a valid connect packet.
    /// </summary>
    public static string? GetUserinfo(byte[] packet)
    {
        if (packet.Length < 5 ||
            packet[0] != 0xFF || packet[1] != 0xFF ||
            packet[2] != 0xFF || packet[3] != 0xFF)
            return null;

        int commandEnd = 4;
        while (commandEnd < packet.Length && packet[commandEnd] != 0x20)
            commandEnd++;
        commandEnd++;

        if (commandEnd >= packet.Length)
            return null;

        byte[] compressedArgs = new byte[packet.Length - commandEnd];
        Array.Copy(packet, commandEnd, compressedArgs, 0, compressedArgs.Length);

        int originalSize = (compressedArgs[0] << 8) | compressedArgs[1];
        byte[] decompressed = JKHuffman.Decompress(compressedArgs, originalSize, startBit: 16);

        return System.Text.Encoding.Latin1.GetString(decompressed);
    }

    /// <summary>
    /// Rebuilds a connect packet with a modified userinfo string.
    /// </summary>
    public static byte[] SetUserinfo(byte[] packet, string userinfo)
    {
        // Find where the compressed args begin
        int commandEnd = 4;
        while (commandEnd < packet.Length && packet[commandEnd] != 0x20)
            commandEnd++;
        commandEnd++;

        // Re-compress the new userinfo
        byte[] raw = System.Text.Encoding.Latin1.GetBytes(userinfo);
        byte[] compressed = JKHuffman.Compress(raw);

        // Rebuild: OOB marker + "connect " + compressed args
        byte[] result = new byte[commandEnd + compressed.Length];
        Array.Copy(packet, 0, result, 0, commandEnd);
        Array.Copy(compressed, 0, result, commandEnd, compressed.Length);
        return result;
    }
}