namespace FinnHubProxy.BusinessObjects
{
    public class PortfolioEmail : Watchlist
    {
        private string Email { get; set; } = string.Empty;

        public PortfolioEmail(string id, string name, string email)
        {
            Id = id; // watchlist Id
            Name = name; // watchlist name
            Email = email; // user email associated with the watchlist
        }
    }
}
