namespace FinnHubProxy.Services;

public class AdminUserStore
{
    private List<string> Users { get; } = new();

    public void AddUser(string username)
    {
        Users.Add(username);
    }

    public bool IsAdmin(string username)
    {
        return Users.Contains(username, StringComparer.OrdinalIgnoreCase);        
    }
}
