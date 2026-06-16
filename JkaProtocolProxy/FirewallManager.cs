using System.Diagnostics;
using System.Runtime.InteropServices;

public static class FirewallManager
{
    private record PortRule(int Port, string Protocol);

    private static readonly PortRule[] Rules =
    [
        new(30000, "UDP"),
        new(29070, "UDP"),
        new(80,    "TCP"),
    ];

    public static void OpenPorts()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ApplyWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            ApplyMacOS();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ApplyLinux();
        else
            throw new PlatformNotSupportedException("Firewall management not supported on this platform.");
    }

    private static void ApplyWindows()
    {
        foreach (var rule in Rules)
        {
            var name = $"OpenJK-{rule.Protocol}-{rule.Port}";
            RunProcess("netsh", $"advfirewall firewall add rule name=\"{name}\" dir=in action=allow protocol={rule.Protocol} localport={rule.Port}");
            RunProcess("netsh", $"advfirewall firewall add rule name=\"{name}-out\" dir=out action=allow protocol={rule.Protocol} localport={rule.Port}");
        }
    }

    private static void ApplyMacOS()
{
    var udpPorts = string.Join(", ", Rules.Where(r => r.Protocol == "UDP").Select(r => r.Port));
    var tcpPorts = string.Join(", ", Rules.Where(r => r.Protocol == "TCP").Select(r => r.Port));

    var pfRules = "";
    if (!string.IsNullOrEmpty(udpPorts)) pfRules += $"pass in proto udp to any port {{{udpPorts}}}\n";
    if (!string.IsNullOrEmpty(tcpPorts)) pfRules += $"pass in proto tcp to any port {{{tcpPorts}}}\n";

    File.WriteAllText("/etc/pf.anchors/openjk", pfRules);
    RunProcess("sh", "-c \"grep -q 'anchor \\\"openjk\\\"' /etc/pf.conf || echo 'anchor \\\"openjk\\\"' >> /etc/pf.conf\"");
    RunProcess("pfctl", "-f /etc/pf.conf");
    RunProcessIgnoreExit("pfctl", "-e"); // exit 1 = already enabled, that's fine
}

    private static void ApplyLinux()
    {
        foreach (var rule in Rules)
        {
            RunProcess("iptables", $"-C INPUT -p {rule.Protocol.ToLower()} --dport {rule.Port} -j ACCEPT 2>/dev/null || iptables -A INPUT -p {rule.Protocol.ToLower()} --dport {rule.Port} -j ACCEPT");
        }
    }

    private static void RunProcess(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var p = Process.Start(psi)!;
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            var err = p.StandardError.ReadToEnd();
            throw new Exception($"[FirewallManager] '{file} {args}' failed (exit {p.ExitCode}): {err}");
        }
    }

    private static void RunProcessIgnoreExit(string file, string args)
{
    var psi = new ProcessStartInfo(file, args)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    using var p = Process.Start(psi)!;
    p.WaitForExit();
    // intentionally not checking exit code
}
}