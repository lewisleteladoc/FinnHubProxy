using System.Collections.Concurrent;
using System.Linq;

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
    private ConcurrentDictionary<string, List<string>> WatchlistSecuritiesStore { get; } = new();
    private ConcurrentDictionary<string, string> Watchlists { get; } = new();

    public void AddToWatchlist(string watchlistId, string symbol)
    {
        // Get existing list or create a new one atomicaly
        var list = WatchlistSecuritiesStore.GetOrAdd(watchlistId, _ => new List<string>());

        // Lock to ensure thread-safety for the List<T> itself
        lock (list)
        {
            if (!list.Contains(symbol))
            {
                list.Add(symbol);
            }
        }
    }
    public void AddToWatchlist(string watchlistId, string[] symbols)
    {
        // Get existing list or create a new one atomicaly
        var list = WatchlistSecuritiesStore.GetOrAdd(watchlistId, _ => new List<string>());

        // Lock to ensure thread-safety for the List<T> itself
        lock (list)
        {
            list.AddRange(symbols.Except(list, StringComparer.OrdinalIgnoreCase));
        }
    }
    public string CreateWatchlist(string watchListName)
    {
        // TryAdd returns true if the key was added, 
        // or false if the userId already exists.
        string watchlistId = Guid.NewGuid().ToString();
        if (Watchlists.TryAdd(watchlistId, watchListName))
        {
            if(WatchlistSecuritiesStore.TryAdd(watchlistId, new List<string>()))
            {
                return watchlistId;
            }
            
        }

        return "";

    }

    public List<string>? GetWatchlistPortfolio(string watchlistId)
    {
        // TryGetValue returns true if the key is found, and assigns the result to 'list'
        if (WatchlistSecuritiesStore.TryGetValue(watchlistId, out var list))
        {
            // Sort and return a NEW list to keep the store's original order intact
            return list.OrderBy(x => x).ToList();
        }

        return null; // Or return new List<string>() depending on your preference
    }

    public dynamic GetWatchlistFullName(List<string> watchlistIds)
    {
        return watchlistIds
        .Where(id => Watchlists.ContainsKey(id))
        .Select(id => new
        {
            id = id,
            name = Watchlists[id]
        })
        .ToList();
    }
}
