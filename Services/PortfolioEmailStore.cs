using System.Collections.Concurrent;
using FinnHubProxy.BusinessObjects;

namespace FinnHubProxy.Services
{
    public class PortfolioEmailStore
    {
        private ConcurrentDictionary<string, PortfolioEmail> portfolios { get; } = new();

        public void AddPortfolioEmail(PortfolioEmail pfe)
        {
            if (!portfolios.ContainsKey(pfe.Id))
            {
                portfolios[pfe.Id] = pfe;
            }
            // portfolios[username].Add(watchlistId);
        }
    }
}
