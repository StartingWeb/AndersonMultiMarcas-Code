using System.Net;
using System.Net.Sockets;

namespace Project.Features.Storage.Legacy;

public static class LegacySourceUrlGuard
{
    public static async Task ValidateAsync(Uri uri, IEnumerable<string> allowedHosts, CancellationToken ct)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("A importacao aceita apenas URLs HTTPS.");
        }

        if (!IsAllowedHost(uri.Host, allowedHosts))
        {
            throw new InvalidOperationException($"Host de origem bloqueado: {uri.Host}.");
        }

        if (IPAddress.TryParse(uri.Host, out var literalAddress))
        {
            EnsurePublicAddress(literalAddress, uri.Host);
            return;
        }

        var addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, ct);
        if (addresses.Length == 0)
        {
            throw new InvalidOperationException($"Host sem resolucao DNS: {uri.Host}.");
        }

        foreach (var address in addresses)
        {
            EnsurePublicAddress(address, uri.Host);
        }
    }

    public static bool IsAllowedHost(string? host, IEnumerable<string> allowedHosts)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return allowedHosts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().TrimEnd('.').ToLowerInvariant())
            .Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 0
                || bytes[0] == 10
                || bytes[0] == 127
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                || bytes[0] >= 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6Multicast
                || address.IsIPv6SiteLocal
                || address.Equals(IPAddress.IPv6Loopback)
                || IsUniqueLocalIpv6(address);
        }

        return true;
    }

    private static void EnsurePublicAddress(IPAddress address, string host)
    {
        if (IsBlockedAddress(address))
        {
            throw new InvalidOperationException($"Host de origem resolve para endereco bloqueado: {host} -> {address}.");
        }
    }

    private static bool IsUniqueLocalIpv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length > 0 && (bytes[0] & 0xfe) == 0xfc;
    }
}
