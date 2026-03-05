using System.Collections.Concurrent;

namespace FinnHubProxy.Services;

public class SecuritiesStoriesStore
{
    // Use ConcurrentDictionary for thread-safe access to the collection
    private readonly ConcurrentDictionary<string, List<string>> securitiesStories = new ConcurrentDictionary<string, List<string>>();

    public void AddStoryToSecurity(string security, string story)
    {
        // Get the existing list or create a new one if it doesn't exist
        var list = securitiesStories.GetOrAdd(security, _ => new List<string>());

        // Lock the list to ensure thread-safe modification of the values
        lock (list)
        {
            if (!list.Contains(story))
            {
                list.Add(story);
            }
        }
    }
}
