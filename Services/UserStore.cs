using System.Collections.Concurrent;

namespace FinnHubProxy.Services;

public class UserStore
{    
    public ConcurrentDictionary<string, string> Users { get; } = new();

    public bool AddUser(string username, string guid)
    {
        // Returns true if the key was added, 
        // returns false if the key already exists.
        return Users.TryAdd(username, guid);

    }       
}
