using DyndnsUpdater.Client.Providers;
using MaSch.Console;

namespace DyndnsUpdater.Client;

public class Program
{
    public static async Task<int> Main()
    {
        var console = new ConsoleService();
        var providers = new IProvider[]
        {
            new CloudflareProvider(console),
        };

        var providerName = EnvironmentVariables.Provider;
        var provider = providers.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            console.WriteLine($"ERROR: Unknown DYNDNS_PROVIDER: {providerName}");
            return 1;
        }

        return await provider.RunAsync();
    }
}
