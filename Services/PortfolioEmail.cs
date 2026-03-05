namespace FinnHubProxy.Services
{
    public class PortfolioEmail : Watchlist
    {
        public string Email { get; set; } = string.Empty;
        public PortfolioEmail(string id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
        }
    }
}
