using System.Text;

public class Helper
{
    public static int FindPattern(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        {
            if (pattern.SequenceEqual(source.Skip(i).Take(pattern.Length)))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Generates a clean Wireshark/Hex-Editor style visualization in the console window.
    /// </summary>
    public static void PrintHexDump(byte[] bytes)
    {
        if (bytes == null) return;

        int bytesPerLine = 16;
        StringBuilder hexSeq = new StringBuilder();
        StringBuilder textSeq = new StringBuilder();

        for (int i = 0; i < bytes.Length; i++)
        {
            // Print the current memory index offset at the start of each line
            if (i % bytesPerLine == 0)
            {
                Console.Write($"{i:D4}: ");
            }

            // Add to the hex visualization block
            hexSeq.Append($"{bytes[i]:X2} ");

            // Add to the human-readable text visualization block on the far right
            char c = (char)bytes[i];
            if (char.IsControl(c) || bytes[i] < 32 || bytes[i] > 126)
            {
                textSeq.Append('.'); // Replace unprintable characters with a dot
            }
            else
            {
                textSeq.Append(c);
            }

            // Extra visual spacer halfway through the line (after 8 bytes)
            if (i % bytesPerLine == 7)
            {
                hexSeq.Append(" ");
            }

            // Line wrap logic once we hit 16 bytes
            if ((i + 1) % bytesPerLine == 0 || i == bytes.Length - 1)
            {
                // If it's the last line and it's short, pad it out to keep columns aligned
                if ((i + 1) % bytesPerLine != 0)
                {
                    int missingBytes = bytesPerLine - ((i + 1) % bytesPerLine);
                    int spacesNeeded = (missingBytes * 3) + (missingBytes > 8 ? 1 : 0);
                    hexSeq.Append(new string(' ', spacesNeeded));
                }

                // Print the hex values followed by the plain text segment
                Console.WriteLine($"{hexSeq}| {textSeq}");
                hexSeq.Clear();
                textSeq.Clear();
            }
        }
    }
}