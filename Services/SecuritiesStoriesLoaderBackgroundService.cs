using System.Security.AccessControl;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace FinnHubProxy.Services
{
    public class SecuritiesStoriesLoaderBackgroundService : BackgroundService
    {
        private readonly ILogger<SecuritiesStoriesLoaderBackgroundService> _logger;
        private readonly SecuritiesStoriesStore securityStore;
        private readonly IWebHostEnvironment env;
        private IConfiguration configuration;

        public SecuritiesStoriesLoaderBackgroundService(
            ILogger<SecuritiesStoriesLoaderBackgroundService> logger,
            IWebHostEnvironment env,
            SecuritiesStoriesStore store,
            IConfiguration configuration)
        {
            _logger = logger;
            securityStore = store;
            this.env = env;
            this.configuration = configuration;
        }

        // Change 1: Use Task instead of void, and IEnumerable<string> for LINQ support
        private async Task<decimal> ProcessSymbolsBatchAsync(IEnumerable<string> symbolsList)
        {
            var finnApi = configuration["FinnApi:BaseUrl"];
            var apiKey = configuration["FinnApi:ApiKey"];
            using var httpClient = new HttpClient();

            decimal batchTotalAum = 0;

            var tasks = symbolsList.Select(symbol =>
                httpClient.GetAsync($"{finnApi}/api/v1/quote?symbol={symbol.ToUpper()}&token={apiKey}")
            );

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                var content = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                // Extract symbol from the request URL
                var queryParams = QueryHelpers.ParseQuery(response.RequestMessage.RequestUri.Query);
                string symbol = queryParams["symbol"];

                if (root.TryGetProperty("c", out JsonElement price))
                {
                    decimal currentPrice = price.GetDecimal();
                    batchTotalAum += currentPrice; // Summing "AUM" (Price) as requested

                    securityStore.AddStoryToSecurity(symbol, $"Current price of: {currentPrice}");
                }
            }

            return batchTotalAum;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StartupFileLoaderService started.");            
            int BATCH_SIZE = 30;
            decimal totalAum = 0;

            try
            {
                // Example: read files from MockData folder
                var basePath = Path.Combine(env.ContentRootPath, "MockData");
                var symbolsPath = Path.Combine(basePath, "symbols.txt");                

                if (File.Exists(symbolsPath))
                {
                    var allSymbols = await File.ReadAllLinesAsync(symbolsPath, stoppingToken);                    

                    // Logic to get 30 elements at a time
                    foreach (var batch in allSymbols.Chunk(BATCH_SIZE))
                    {
                        totalAum += await ProcessSymbolsBatchAsync(batch);

                        await Task.Delay(1000, stoppingToken);
                    }                    
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading startup files.");
            }

            _logger.LogInformation("StartupFileLoaderService finished. " + totalAum);

            // If this should run only once, stop here:
            await Task.CompletedTask;

            // If you wanted a loop instead:
            // while (!stoppingToken.IsCancellationRequested)
            // {
            //     await Task.Delay(10000, stoppingToken);
            // }
        }
    }
}