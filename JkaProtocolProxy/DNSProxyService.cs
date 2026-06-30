using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;

namespace JkaProtocolProxy
{
    public class DNSProxyService
    {
        private const int DnsPort = 53;
        private const string TargetDomain = "gateway.sw-jkja-mp.eks.aspyr.com";
        private const string UpstreamDns = "8.8.8.8";

        public async Task RunDNSUdpListener(string spoofedIp, CancellationToken ct)
        {
            using UdpClient listener = new UdpClient(new IPEndPoint(IPAddress.Any, DnsPort));
            Console.WriteLine("[SUCCESS] Listening on UDP Port 53.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult receivedResult = await listener.ReceiveAsync();
                    byte[] requestBytes = receivedResult.Buffer;

                    IRequest request = Request.FromArray(requestBytes);

                    if (request.Questions != null && request.Questions.Count > 0)
                    {
                        string queriedDomain = request.Questions[0].Name.ToString().TrimEnd('.');

                        if (queriedDomain.Equals(TargetDomain, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[DNS UDP] Intercepted Request: {queriedDomain} Spoofing response pointing to -> {spoofedIp}");

                            Response response = Response.FromRequest(request);
                            IResourceRecord record = new IPAddressResourceRecord(
                                request.Questions[0].Name,
                                IPAddress.Parse(spoofedIp)
                            );
                            response.AnswerRecords.Add(record);

                            byte[] responseBytes = response.ToArray();
                            await listener.SendAsync(responseBytes, responseBytes.Length, receivedResult.RemoteEndPoint);
                        }
                        else
                        {
                            using UdpClient forwarder = new UdpClient();
                            IPEndPoint upstreamEndPoint = new IPEndPoint(IPAddress.Parse(UpstreamDns), DnsPort);

                            await forwarder.SendAsync(requestBytes, requestBytes.Length, upstreamEndPoint);
                            UdpReceiveResult upstreamResult = await forwarder.ReceiveAsync();
                            byte[] upstreamResponseBytes = upstreamResult.Buffer;

                            await listener.SendAsync(upstreamResponseBytes, upstreamResponseBytes.Length, receivedResult.RemoteEndPoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[DNS UDP] Skipped malformed packet: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        public async Task RunDNSTcpListener(string spoofedIp, CancellationToken ct)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, DnsPort);
            listener.Start();
            Console.WriteLine("[DNS] Listening on TCP Port 53.");

            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleTcpClient(client, spoofedIp));
            }
        }

        public async Task HandleTcpClient(TcpClient client, string spoofedIp)
        {
            using (client)
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    // TCP DNS prefixes each message with a 2-byte big-endian length
                    byte[] lengthBuf = new byte[2];
                    await stream.ReadExactlyAsync(lengthBuf, 0, 2);
                    int msgLength = (lengthBuf[0] << 8) | lengthBuf[1];

                    byte[] requestBytes = new byte[msgLength];
                    await stream.ReadExactlyAsync(requestBytes, 0, msgLength);

                    IRequest request = Request.FromArray(requestBytes);

                    if (request.Questions == null || request.Questions.Count == 0)
                    {
                        Console.WriteLine("[DNS TCP] Received request with no questions, dropping.");
                        return;
                    }

                    string queriedDomain = request.Questions[0].Name.ToString().TrimEnd('.');

                    if (queriedDomain.Equals(TargetDomain, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[DNS TCP] Intercepted Request: {queriedDomain} Spoofing response pointing to -> {spoofedIp}");

                        Response response = Response.FromRequest(request);
                        IResourceRecord record = new IPAddressResourceRecord(
                            request.Questions[0].Name,
                            IPAddress.Parse(spoofedIp)
                        );
                        response.AnswerRecords.Add(record);

                        byte[] responseBytes = response.ToArray();

                        byte[] respLen = new byte[2];
                        respLen[0] = (byte)(responseBytes.Length >> 8);
                        respLen[1] = (byte)(responseBytes.Length & 0xFF);

                        await stream.WriteAsync(respLen);
                        await stream.WriteAsync(responseBytes);
                    }
                    else
                    {
                        using TcpClient upstream = new TcpClient();
                        await upstream.ConnectAsync(IPAddress.Parse(UpstreamDns), DnsPort);
                        NetworkStream upstreamStream = upstream.GetStream();

                        await upstreamStream.WriteAsync(lengthBuf);
                        await upstreamStream.WriteAsync(requestBytes);

                        byte[] upstreamLenBuf = new byte[2];
                        await upstreamStream.ReadExactlyAsync(upstreamLenBuf, 0, 2);
                        int upstreamLen = (upstreamLenBuf[0] << 8) | upstreamLenBuf[1];

                        byte[] upstreamResponseBytes = new byte[upstreamLen];
                        await upstreamStream.ReadExactlyAsync(upstreamResponseBytes, 0, upstreamLen);

                        await stream.WriteAsync(upstreamLenBuf);
                        await stream.WriteAsync(upstreamResponseBytes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DNS TCP] Client handling error: {ex.Message}");
                }
            }
        }
    }
}
