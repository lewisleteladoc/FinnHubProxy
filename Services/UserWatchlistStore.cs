using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace FinnHubProxy.Services;

// This is like the relation database table that links users to watchlists.
public class UserWatchlistStore
{
    private ConcurrentDictionary<string, List<string>> UserWatchlists =
        new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<UserWatchlistStore> logger;

    public UserWatchlistStore(ILogger<UserWatchlistStore> logger)
    {
        this.logger = logger;
    }
    public void AddUserWatchlistRelationship(string username, string watchlistId)
    {
        // Returns true if the key was added, 
        // returns false if the key already exists.
        if (!UserWatchlists.ContainsKey(username))
        {
            UserWatchlists[username] = new List<string>();
        }
        UserWatchlists[username].Add(watchlistId);
        // logger.LogInformation($"username {username} - watchlistId {watchlistId}");
    }

    public bool RemoveUserWatchlistRelationship(string username, string watchlistId, bool isAdmin)
    {
        // Returns true if the key was added, 
        // returns false if the key already exists.
        if (isAdmin)
        {
            List<string> allWatchLists = UserWatchlists.Values.SelectMany(x => x).ToList();
            return allWatchLists.Remove(watchlistId);
        }

        if (UserWatchlists.TryGetValue(username, out var list))
        {
            return list.Remove(watchlistId);            
        }
        return false;
    }

    private List<string>? HelperGetUserWatchlists(string userId, bool isAdmin = false)
    {
        if (isAdmin)
        {
            return UserWatchlists.Values.SelectMany(x => x).ToList();
        }
        // Try to get the list associated with the key
        if (UserWatchlists.TryGetValue(userId, out var list))
        {
            return list; // Found! Returns the List<string>
        }

        return null; // Not found
    }

    public List<string>? GetUserWatchlists(string userId, bool isAdmin = false)
    {
       return HelperGetUserWatchlists(userId, isAdmin);
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
