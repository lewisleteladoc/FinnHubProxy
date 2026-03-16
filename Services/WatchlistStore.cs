using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using FinnHubProxy.Services;
using Microsoft.VisualBasic;
using System.Data;


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
    private ConcurrentDictionary<string, List<string>> WatchlistSecuritiesStore { get; }
    = new(StringComparer.OrdinalIgnoreCase);
    // WatchlistSecuritiesStore
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
    public void AddToWatchlistInBulk(string watchlistId, string[] symbols)
    {
        // Get existing list or create a new one atomicaly
        var list = WatchlistSecuritiesStore.GetOrAdd(watchlistId, _ => new List<string>());

        // Lock to ensure thread-safety for the List<T> itself
        lock (list)
        {
            list.AddRange(symbols.Except(list, StringComparer.OrdinalIgnoreCase));
        }
    }
    public string CreateWatchlist(string watchListName, string username ="", bool isManulCreation = false)
    {
        // var username = httpContextAccessor.HttpContext?.Items["username"]?.ToString();
        // var token = httpContextAccessor.HttpContext?.Items["token"]?.ToString();

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

    public bool WatchlistExists(string watchlistid)
    {
        foreach (var wl in Watchlists)
        {
            if (wl.Key == watchlistid)
            {
                return true;
            }
        }
        return false;
    }

    public bool DeleteWatchlist(string watchlistid)
    {
        if(Watchlists.TryRemove(watchlistid, out var list))
        {
            WatchlistSecuritiesStore.TryRemove(watchlistid, out var strings);
            return true;
        } 
        return false;
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

    public dynamic GetWatchlistWithSymbol(List<string> watchlistIds, string symbol)
    {
        return watchlistIds
        .Where(id => WatchlistSecuritiesStore.TryGetValue(id, out var securities) &&
                     securities.Contains(symbol, StringComparer.OrdinalIgnoreCase))
        .ToList();

        // bad performance...
        /* var securities = watchlistIds
        .Where(id => WatchlistSecuritiesStore.TryGetValue(id, out var securities));

        return securities.Contains(symbol, StringComparer.OrdinalIgnoreCase); */
    }
}
