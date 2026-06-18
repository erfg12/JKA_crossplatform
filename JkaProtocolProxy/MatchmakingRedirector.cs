using System.Net;
using System.Net.Sockets;
using System.Text;

class MatchmakingRedirector
{
    private static readonly byte[] OobMarker = { 0xFF, 0xFF, 0xFF, 0xFF };
    private const string TargetRequestString = "startmatchmaking";

    /// <summary>
    /// Asynchronously listens for standard HTTP requests on Port 80 and answers with a 200 OK status.
    /// </summary>
    public static async Task StartHealthListener(int port, CancellationToken ct)
    {
        if (!HttpListener.IsSupported)
        {
            Console.WriteLine("[HTTP - ERROR] HttpListener is not supported on this operating system.");
            return;
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");

        try
        {
            listener.Start();
            Console.WriteLine($"[HTTP - INIT] Web Server bound to TCP port {port}. Awaiting background web check-ins...\n");

            ct.Register(() => listener.Stop());

            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Console.WriteLine($"[HTTP - INBOUND] Intercepted Health Check Request {request.Url} -> {request.RemoteEndPoint}");

                // Basic 200 OK response payload
                string responseString = "<html><body>200 OK</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                response.StatusCode = (int)HttpStatusCode.OK;

                using (Stream output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                }

                Console.WriteLine($"[HTTP - RESPONSE] Sent Status 200 OK back to {request.RemoteEndPoint}.\n");
            }
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"[HTTP - CRITICAL] Could not bind to TCP port {port}.");
            Console.WriteLine("  -> IMPORTANT: Windows requires Administrator privileges to bind to Port 80.");
            Console.WriteLine($"  -> Error Message: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HTTP - ERROR] Exception in web loop: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously handles the JKA UDP matchmaking redirection loop.
    /// </summary>
    public static async Task StartUdpListenerAsync(int listenPort, string targetIp, ushort targetPort, CancellationToken ct)
    {
        try
        {
            using (var udpClient = new UdpClient(listenPort))
            {
                Console.WriteLine($"[UDP - INIT] Socket bound to UDP port {listenPort}. Awaiting matchmaking traffic...\n");

                ct.Register(() => udpClient.Close());

                while (!ct.IsCancellationRequested)
                {
                    UdpReceiveResult receiveResult = await udpClient.ReceiveAsync();
                    byte[] incomingData = receiveResult.Buffer;
                    IPEndPoint clientEndPoint = receiveResult.RemoteEndPoint;

                    if (incomingData.Length < OobMarker.Length + TargetRequestString.Length)
                        continue;

                    if (incomingData[0] != OobMarker[0] || incomingData[1] != OobMarker[1] ||
                        incomingData[2] != OobMarker[2] || incomingData[3] != OobMarker[3])
                    {
                        continue;
                    }

                    string payloadText = Encoding.ASCII.GetString(incomingData, OobMarker.Length, incomingData.Length - OobMarker.Length);

                    if (payloadText.StartsWith(TargetRequestString, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[UDP - MATCH] Handshaking request from console client: {clientEndPoint}");

                        byte[] responseBytes = BuildResponsePayload(targetIp, targetPort);
                        await udpClient.SendAsync(responseBytes, responseBytes.Length, clientEndPoint);

                        Console.WriteLine($"[UDP - REPLY] Dispatched corrected matchmaking redirect payload to {clientEndPoint}.\n");
                        //Console.WriteLine(new string('-', 80));
                    }
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[UDP - CRITICAL] Could not bind to port {listenPort}. Port may be locked by another application. Message: {ex.Message}");
        }
    }

    /// <summary>
    /// Constructs the corrected byte stream payload for the matchmaking redirection response.
    /// Format on the wire: [OOB_Marker][ResponseCommandString]\[IP_Bytes][Port_ShortLE]\EOT
    /// </summary>
    private static byte[] BuildResponsePayload(string targetIp, ushort targetPort)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // Write connectionless signature (\xFF\xFF\xFF\xFF)
            writer.Write(OobMarker);

            // Write command text string up to the leading backslash
            byte[] commandBytes = Encoding.ASCII.GetBytes("startmatchmakingResponse \\");
            writer.Write(commandBytes);

            // Parse and write 4 raw IP bytes
            IPAddress parsedIp = IPAddress.Parse(targetIp);
            byte[] ipBytes = parsedIp.GetAddressBytes();
            writer.Write(ipBytes);

            // Serialize 16-bit short port in Little-Endian configuration
            writer.Write(targetPort);

            // Finalize with End of Transmission string token
            byte[] eotBytes = Encoding.ASCII.GetBytes("\\EOT");
            writer.Write(eotBytes);

            return ms.ToArray();
        }
    }
}