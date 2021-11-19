using System.Net;

namespace DyndnsUpdater.Client;

public static class EnvironmentVariables
{
    public static readonly string ProviderKey = "DYNDNS_PROVIDER";
    public static readonly string UpdaterUrlKey = "DYNDNS_UPDATERURL";
    public static readonly string CheckIntervalKey = "DYNDNS_CHECKINTERVAL";
    public static readonly string DnsIpKey = "DYNDNS_DNSIP";
    public static readonly string CloudflareZoneKey = "DYNDNS_CLOUDFLARE_ZONE";
    public static readonly string CloudflareRecordKey = "DYNDNS_CLOUDFLARE_RECORD";
    public static readonly string CloudflareTokenKey = "DYNDNS_CLOUDFLARE_TOKEN";

    private static string? _provider;
    private static Uri? _updaterUri;
    private static int? _checkInterval;
    private static IPAddress? _dnsIp;
    private static string? _cloudflareZone;
    private static string? _cloudflareRecord;
    private static string? _cloudflareToken;

    public static string Provider => _provider ??= GetRequiredEnvironmentVariable(ProviderKey);
    public static Uri UpdaterUri => _updaterUri ??= 
        Uri.TryCreate(GetRequiredEnvironmentVariable(UpdaterUrlKey).Trim('/') + "/", UriKind.Absolute, out var uri)
            ? uri
            : throw new ArgumentException($"Please provide a valid URI as {UpdaterUrlKey}", (Exception?)null);
    public static int CheckInterval => _checkInterval ??= int.TryParse(Environment.GetEnvironmentVariable(CheckIntervalKey), out var interval) ? interval : 60_000;
    public static IPAddress DnsIp => _dnsIp ??= IPAddress.TryParse(Environment.GetEnvironmentVariable(DnsIpKey), out var ip) ? ip : new IPAddress(new byte[] { 8, 8, 8, 8 });
    public static string CloudflareZone => _cloudflareZone ??= GetRequiredEnvironmentVariable(CloudflareZoneKey);
    public static string CloudflareRecord => _cloudflareRecord ??= GetRequiredEnvironmentVariable(CloudflareRecordKey);
    public static string CloudflareToken => _cloudflareToken ??= GetRequiredEnvironmentVariable(CloudflareTokenKey);

    public static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException($"Please provide Environment variable {name}", (Exception?)null);
        return value;
    }
}
