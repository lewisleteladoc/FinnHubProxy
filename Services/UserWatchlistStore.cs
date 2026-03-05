using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace FinnHubProxy.Services;

// This is like the relation database table that links users to watchlists.
public class UserWatchlistStore
{
    private ConcurrentDictionary<string, List<string>> UserWatchlists { get; } = new();

    public void AddUserWatchlistRelationship(string username, string watchlistId)
    {
        // Returns true if the key was added, 
        // returns false if the key already exists.
        if (!UserWatchlists.ContainsKey(username))
        {
            UserWatchlists[username] = new List<string>();
        }
        UserWatchlists[username].Add(watchlistId);        

    }

    public List<string>? GetUserWatchlists(string userId)
    {
        // Try to get the list associated with the key
        if (UserWatchlists.TryGetValue(userId, out var list))
        {
            return list; // Found! Returns the List<string>
        }

        return null; // Not found
    }

    public bool UserHasWatchlistId(string username, string watchlistId)
    {
        if (UserWatchlists.TryGetValue(username, out var watchlists))
        {
            bool exists = watchlists.Contains(watchlistId);
            return exists;
        }

        return false;
    }    
}
