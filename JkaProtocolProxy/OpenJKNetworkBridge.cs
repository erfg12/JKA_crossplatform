using System;
using System.IO;
using System.IO.Pipes;

public class OpenJKNetworkBridge
{
    /// <summary>
    /// Sends the raw, unmodified network packet directly to OpenJK via Named Pipes
    /// and returns the newly modified, re-compressed binary payload frame.
    /// </summary>
    public static byte[] ModifyAndReconstructPacket(byte[] rawWiresharkPacket)
    {
        if (rawWiresharkPacket == null || rawWiresharkPacket.Length == 0) return null;

        try
        {
            using (var pipeClient = new NamedPipeClientStream(".", "OpenJKDecoderPipe", PipeDirection.InOut))
            {
                pipeClient.Connect(2000); // 2 second timeout
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                // Send the ENTIRE packet (including headers) straight to OpenJK
                pipeClient.Write(rawWiresharkPacket, 0, rawWiresharkPacket.Length);
                pipeClient.Flush();

                // Read the freshly modified/re-compressed game payload returned by OpenJK
                using (var memoryStream = new MemoryStream())
                {
                    byte[] responseBuffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = pipeClient.Read(responseBuffer, 0, responseBuffer.Length);
                        if (bytesRead > 0)
                        {
                            memoryStream.Write(responseBuffer, 0, bytesRead);
                        }
                    }
                    while (!pipeClient.IsMessageComplete && bytesRead > 0);

                    return memoryStream.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Pipe Error] {ex.Message}");
            return null;
        }
    }
}