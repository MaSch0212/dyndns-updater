using DnsClient;
using DnsClient.Protocol;
using System.Net;

namespace DyndnsUpdater.Client;

public class Program
{
    private static string _updaterUrl = null!;
    private static int _checkInterval = Math.Max(5000, int.TryParse(Environment.GetEnvironmentVariable("DYNDNS_CHECKINTERVAL"), out var interval) ? interval : 60_000);
    private static IPAddress _dnsIp = IPAddress.TryParse(Environment.GetEnvironmentVariable("DYNDNS_DNSIP"), out var dns) ? dns : new IPAddress(new byte[] { 8, 8, 8, 8 });
    private static LookupClient _lookup = new(_dnsIp);

    public static async Task Main(string[] args)
    {
        var provider = GetRequiredEnvironmentVariable("DYNDNS_PROVIDER");
        _updaterUrl = GetRequiredEnvironmentVariable("DYNDNS_UPDATERURL")!;

        Console.WriteLine($"Provider: {provider}");
        Console.WriteLine($"Updater URL: {_updaterUrl}");
        Console.WriteLine($"Check Interval: {_checkInterval}");
        Console.WriteLine($"DNS IP: {_dnsIp}");

        switch (provider?.ToLower())
        {
            case "cloudflare":
                await HandleCloudflare();
                break;
            default:
                throw new Exception($"ERROR: Unknown DYNDNS_PROVIDER: {provider}");
        }
    }

    private static async Task HandleCloudflare()
    {
        var zone = GetRequiredEnvironmentVariable("DYNDNS_CLOUDFLARE_ZONE");
        var record = GetRequiredEnvironmentVariable("DYNDNS_CLOUDFLARE_RECORD");
        var token = GetRequiredEnvironmentVariable("DYNDNS_CLOUDFLARE_TOKEN");
        var domainName = record.EndsWith(zone) ? record : $"{record}.{zone}";

        Console.WriteLine($"Domain Name: {domainName}");
        Console.WriteLine($"Cloudflare Zone Name: {zone}");
        Console.WriteLine($"Cloudflare Record Name: {record}");
        Console.WriteLine($"Cloudflare Token: {new string('*', token.Length)}");

        Console.WriteLine();
        Console.WriteLine("Starting to scan for IP Address changes...");
        Console.WriteLine();

        using var httpClient = new HttpClient();
        var lastInfo = DateTime.MinValue;
        while (true)
        {
            try
            {
                var currentDomainIp = await GetCurrentIpAddressAsync(domainName);
                var currentPublicIp = await GetPublicIpAsync(httpClient);

                if (!Equals(currentDomainIp, currentPublicIp))
                {
                    Console.WriteLine($"Public IP ({currentPublicIp}) and Domain IP ({currentDomainIp}) are different");

                    var response = await httpClient.GetAsync($"{_updaterUrl.TrimEnd('/')}/Cloudflare?token={token}&zone={zone}&record={record}&ipv4={currentPublicIp}&proxied=false");
                    response.EnsureSuccessStatusCode();

                    await Task.Delay(5000);
                    currentDomainIp = await GetPublicIpAsync(httpClient);
                    if (Equals(currentDomainIp, currentPublicIp))
                        Console.WriteLine($"IP of domain {domainName} Successfully updated to {currentPublicIp}");
                    else
                        Console.WriteLine($"The DNS did not return correct URL 5 seconds after changing IP.");
                }
                else if ((DateTime.Now - lastInfo).TotalHours > 1)
                {
                    Console.WriteLine($"IP Address of domain {domainName} is still up-to-date: {currentDomainIp}");
                    lastInfo = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured: " + ex);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_checkInterval));
        }
    }

    private static async Task<IPAddress> GetCurrentIpAddressAsync(string domain)
    {
        return ((ARecord)(await _lookup.QueryAsync(domain, QueryType.A)).Answers.First()).Address;
    }

    private static async Task<IPAddress> GetPublicIpAsync(HttpClient httpClient)
    {
        var ipv4 = await httpClient.GetStringAsync("http://ipinfo.io/ip");
        return IPAddress.Parse(ipv4);
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException($"Please provide Environment variable {name}", (Exception?)null);
        return value;
    }
}
