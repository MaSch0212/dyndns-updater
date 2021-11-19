using MaSch.Console;
using System.Net;

namespace DyndnsUpdater.Client.Providers;

public sealed class CloudflareProvider : BaseProvider
{
    public CloudflareProvider(IConsoleService console)
        : base(console, "cloudflare")
    {
    }

    private static string Zone => EnvironmentVariables.CloudflareZone;
    private static string Record => EnvironmentVariables.CloudflareRecord;
    private static string Token => EnvironmentVariables.CloudflareToken;

    protected override string DomainName => Record.EndsWith(Zone) ? Record : $"{Record}.{Zone}";

    protected override Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    protected override void OnPrintArguments()
    {
        Console.WriteLine($"Cloudflare Zone Name: {Zone}");
        Console.WriteLine($"Cloudflare Record Name: {Record}");
        Console.WriteLine($"Cloudflare Token: {new string('*', Token.Length)}");
    }

    protected override async Task OnUpdateDynDns(IPAddress currentPublicIp)
    {
        var response = await HttpClient.GetAsync(new Uri(UpdaterUri, $"Cloudflare?token={Token}&zone={Zone}&record={Record}&ipv4={currentPublicIp}&proxied=false"));
        response.EnsureSuccessStatusCode();
    }
}
