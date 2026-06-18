using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JKAServerBrowser
{
    public class ServerEntry
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public override string ToString() => $"{Address}:{Port}";
    }

    public class ServerInfo
    {
        public Dictionary<string, string> Properties { get; set; } = new();
        public string Address { get; set; }
        public int Port { get; set; }
        public int PlayerCount { get; set; }

        public string HostName => StripColors(Properties.GetValueOrDefault("hostname", "Unknown"));
        public string Mod => Properties.GetValueOrDefault("game", "basejka");
        public int Clients => int.TryParse(Properties.GetValueOrDefault("clients"), out var v) ? v : 0;
        public int MaxClients => int.TryParse(Properties.GetValueOrDefault("sv_maxclients"), out var v) ? v : 0;
        public bool Password => Properties.GetValueOrDefault("needpass") == "1";
        public string MapName => Properties.GetValueOrDefault("mapname", "Unknown");
        public string Version
        {
            get => Properties.GetValueOrDefault("protocol") switch
            {
                "26" => "JA 1.01",
                "25" => "JA 1.00",
                "16" => "JO 1.04",
                "15" => "JO 1.02",
                _ => "Unknown"
            };
        }

        private static string StripColors(string s) =>
    System.Text.RegularExpressions.Regex.Replace(s, @"\^\S", "");

        public string GameType
        {
            get => Properties.GetValueOrDefault("gametype") switch
            {
                "0" => "FFA",
                "3" => "Duel",
                "4" => "Power Duel",
                "6" => "TDM",
                "7" => "Siege",
                "8" => "CTF",
                _ => "Unknown"
            };
        }

        public override string ToString() =>
            $"[{Address}:{Port}] {HostName} | {MapName} | {GameType} | {Clients}/{MaxClients} | {Version} | {Mod}";
    }

    public class JKAMasterClient : IDisposable
    {
        private static readonly byte[] GetServersPacket =
            Encoding.Latin1.GetBytes("\xFF\xFF\xFF\xFFgetservers 26\0");

        private static readonly byte[] GetInfoPacket =
            Encoding.Latin1.GetBytes("\xFF\xFF\xFF\xFFgetinfo\0");

        private static readonly byte[] InfoResponseHeader =
            Encoding.Latin1.GetBytes("\xFF\xFF\xFF\xFFinfoResponse\n");

        private static readonly byte[] ServersResponseHeader =
            Encoding.Latin1.GetBytes("\xFF\xFF\xFF\xFFgetserversResponse");

        private readonly UdpClient _udp;

        public static readonly (string Host, int Port)[] MasterServers =
        {
            ("master.jkhub.org", 29060),
            ("104.40.23.123",    29060),
        };

        public JKAMasterClient(int localPort = 0)
        {
            _udp = new UdpClient(localPort);
            _udp.Client.ReceiveTimeout = 5000;
        }

        public async Task<List<ServerEntry>> GetServerListAsync(string masterHost, int masterPort)
        {
            var endpoint = new IPEndPoint(
                (await Dns.GetHostAddressesAsync(masterHost))[0],
                masterPort
            );

            await _udp.SendAsync(GetServersPacket, GetServersPacket.Length, endpoint);

            var servers = new List<ServerEntry>();

            try
            {
                while (true)
                {
                    var result = await _udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(3));
                    var data = result.Buffer;

                    if (!StartsWith(data, ServersResponseHeader))
                        continue;

                    // Data after header: \IP(4B)PORT(2B)\IP(4B)PORT(2B)...EOT\
                    int i = ServersResponseHeader.Length;
                    while (i + 6 < data.Length)
                    {
                        // Each entry is preceded by a backslash
                        if (data[i] == '\\')
                            i++;

                        if (i + 6 > data.Length)
                            break;

                        // Check for EOT marker
                        if (data[i] == 'E' && data[i + 1] == 'O' && data[i + 2] == 'T')
                            break;

                        string ip = $"{data[i]}.{data[i + 1]}.{data[i + 2]}.{data[i + 3]}";
                        int port = (data[i + 4] << 8) | data[i + 5];

                        servers.Add(new ServerEntry { Address = ip, Port = port });
                        i += 6;
                    }
                }
            }
            catch (TimeoutException) { /* done receiving */ }
            catch (OperationCanceledException) { /* done receiving */ }

            return servers;
        }

        public async Task<ServerInfo?> GetServerInfoAsync(string address, int port)
        {
            // Each call gets its own socket on an ephemeral port
            using var udp = new UdpClient(0);
            udp.Client.ReceiveTimeout = 3000;

            var endpoint = new IPEndPoint(IPAddress.Parse(address), port);
            await udp.SendAsync(GetInfoPacket, GetInfoPacket.Length, endpoint);

            try
            {
                var result = await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(3));
                var data = result.Buffer;

                if (!StartsWith(data, InfoResponseHeader))
                    return null;

                var body = Encoding.Latin1.GetString(data, InfoResponseHeader.Length,
                                data.Length - InfoResponseHeader.Length);
                var parts = body.TrimStart('\\').Split('\\');
                var info = new ServerInfo { Address = address, Port = port };

                for (int i = 0; i + 1 < parts.Length; i += 2)
                    info.Properties[parts[i]] = parts[i + 1];

                // Temporary debug dump
                //foreach (var kv in info.Properties)
                //    Console.WriteLine($"  '{kv.Key}' = '{kv.Value}'");

                return info;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ServerInfo>> QueryAllServersAsync(
            List<ServerEntry> servers,
            int maxConcurrency = 20,
            Action<ServerInfo>? onResult = null)
        {
            var results = new List<ServerInfo>();
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task>();

            foreach (var server in servers)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var info = await GetServerInfoAsync(server.Address, server.Port);

                        if (info != null)
                        {
                            lock (results)
                            {
                                results.Add(info);
                                onResult?.Invoke(info);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results;
        }

        private static bool StartsWith(byte[] data, byte[] header)
        {
            if (data.Length < header.Length) return false;
            for (int i = 0; i < header.Length; i++)
                if (data[i] != header[i]) return false;
            return true;
        }

        public void Dispose() => _udp.Dispose();
    }
}