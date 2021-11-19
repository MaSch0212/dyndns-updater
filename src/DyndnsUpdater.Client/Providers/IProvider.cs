namespace DyndnsUpdater.Client.Providers;

public interface IProvider : IDisposable
{
    string Name { get; }

    Task<int> RunAsync();
}
