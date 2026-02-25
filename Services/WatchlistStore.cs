using System.Collections.Concurrent;

namespace FinnHubProxy.Services;

public class WatchListStore
{
    // The private class defined within WatchListStore
    private class Portfolio
    {
        public string PortfolioName { get; set; } = string.Empty;
        public string PortfolioId { get; set; } = string.Empty;   
    }
    // Changed to PascalCase per C# standards
    public ConcurrentDictionary<string, List<string>> WatchlistsStore { get; } = new();
    public ConcurrentDictionary<string, string> Watchlists { get; } = new();

    public void AddToWatchlist(string watchlistId, string symbol)
    {
        // Get existing list or create a new one atomicaly
        var list = WatchlistsStore.GetOrAdd(watchlistId, _ => new List<string>());

        // Lock to ensure thread-safety for the List<T> itself
        lock (list)
        {
            if (!list.Contains(symbol))
            {
                list.Add(symbol);
            }
        }
    }
    public string CreateWatchlist(string watchListName)
    {
        // TryAdd returns true if the key was added, 
        // or false if the userId already exists.
        string guidString = Guid.NewGuid().ToString();
        if (Watchlists.TryAdd(watchListName, guidString))
        {
            if(WatchlistsStore.TryAdd(guidString, new List<string>()))
            {
                return guidString;
            }
            
        }

        return "";

    }

    public List<string>? GetWatchlistPortfolio(string watchlistId)
    {
        // TryGetValue returns true if the key is found, and assigns the result to 'list'
        if (WatchlistsStore.TryGetValue(watchlistId, out var list))
        {
            return list;
        }

        return null; // Or return new List<string>() depending on your preference
    }
}
