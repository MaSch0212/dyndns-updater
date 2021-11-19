using DnsClient;
using DnsClient.Protocol;
using MaSch.Console;
using MaSch.Core;
using System.Diagnostics;
using System.Net;

namespace DyndnsUpdater.Client.Providers;

public abstract class BaseProvider : IProvider
{
    private LookupClient? _lookupClient;
    private HttpClient? _httpClient;

    public BaseProvider(IConsoleService console, string providerName)
    {
        Console = Guard.NotNull(console, nameof(console));
        Name = Guard.NotNullOrEmpty(providerName, nameof(providerName));
    }

    protected static IPAddress DnsIp => EnvironmentVariables.DnsIp;
    protected static int CheckInterval => EnvironmentVariables.CheckInterval;
    protected static Uri UpdaterUri => EnvironmentVariables.UpdaterUri;

    public string Name { get; }

    protected IConsoleService Console { get; }
    protected LookupClient LookupClient => _lookupClient ??= new LookupClient(DnsIp);
    protected HttpClient HttpClient => _httpClient ??= new HttpClient();
    protected abstract string DomainName { get; }

    public async Task<int> RunAsync()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR occured during provider initialization:\n{ex}");
            return 2;
        }

        try
        {
            PrintArguments();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR occured during printing arguments: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Starting to scan for IP Address changes...");
        Console.WriteLine();

        var lastInfoTimestamp = DateTime.MinValue;
        while (true)
        {
            try
            {
                var currentDomainIp = await GetDomainIpAsync();
                var currentPublicIp = await GetPublicIpAsync();

                if (!Equals(currentDomainIp, currentPublicIp))
                {
                    Console.WriteLine($"Public IP ({currentPublicIp}) and Domain IP ({currentDomainIp}) are different");

                    await OnUpdateDynDns(currentPublicIp);

                    Console.WriteLine($"Waiting for DNS to return IP {currentPublicIp} for Domain {DomainName}...");
                    var sw = Stopwatch.StartNew();
                    while (!Equals(currentDomainIp, currentPublicIp) && sw.Elapsed < TimeSpan.FromMinutes(30))
                    {
                        await Task.Delay(5000);
                        currentDomainIp = await GetDomainIpAsync();
                    }

                    if (Equals(currentDomainIp, currentPublicIp))
                        Console.WriteLine($"IP of domain {DomainName} Successfully updated to {currentDomainIp}");
                    else
                        Console.WriteLine($"The DNS did not return the correct IP after 30 Minutes");
                    lastInfoTimestamp = DateTime.Now;
                }
                else if ((DateTime.Now - lastInfoTimestamp).TotalHours > 1)
                {
                    Console.WriteLine($"IP Address of domain {DomainName} is still up-to-date: {currentDomainIp}");
                    lastInfoTimestamp = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR occured: " + ex);
            }

            await Task.Delay(CheckInterval);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }

    protected virtual async Task<IPAddress> GetDomainIpAsync()
    {
        return ((ARecord)(await LookupClient.QueryAsync(DomainName, QueryType.A)).Answers[0]).Address;
    }

    protected virtual async Task<IPAddress> GetPublicIpAsync()
    {
        var ipv4 = await HttpClient.GetStringAsync("http://ipinfo.io/ip");
        return IPAddress.Parse(ipv4);
    }

    protected abstract Task OnInitializeAsync();

    protected abstract void OnPrintArguments();

    protected abstract Task OnUpdateDynDns(IPAddress currentPublicIp);

    private async Task InitializeAsync()
    {
        await OnInitializeAsync();
    }

    private void PrintArguments()
    {
        Console.WriteLine($"Provider: {Name}");
        Console.WriteLine($"Updater URL: {UpdaterUri}");
        Console.WriteLine($"Check Interval: {CheckInterval}");
        Console.WriteLine($"DNS IP: {DnsIp}");
        Console.WriteLine($"Domain Name: {DomainName}");
        OnPrintArguments();
    }
}