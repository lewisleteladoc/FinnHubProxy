using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using FinnHubProxy.Services;

namespace FinnHubProxy.Controllers
{
    [ApiController]
    [Route("api/stocklookup")]
    public class StockLookUpController : ControllerBase
    {
        private readonly WatchListStore currentStore;
        private readonly ILogger<StockLookUpController> _logger;
        private readonly IConfiguration _configuration;

        // FIX: Combine all parameters into this single constructor
        public StockLookUpController(
            ILogger<StockLookUpController> logger,
            IConfiguration configuration,
            WatchListStore store)
        {
            _logger = logger;
            _configuration = configuration;
            currentStore = store;
        } 

        [HttpGet]
        public async Task<IActionResult> GetAsync(string symbol)
        {
            var finnApi = _configuration["FINN_API"];
            var apiKey = _configuration["FINN_API_KEY"];
            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(finnApi + "/api/v1/search?q=" + symbol + "&exchange=US&token=" + apiKey);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();

                return Ok(content);

            }
            else
            {

                return StatusCode((int)response.StatusCode, "Error fetching stock data: " + response.ReasonPhrase);
            }

        }

        [HttpGet("price")]
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            var finnApi = _configuration["FINN_API"];
            var apiKey = _configuration["FINN_API_KEY"];
            var url = $"{finnApi}/api/v1/quote?symbol={symbol}&token={apiKey}";

            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();

                return Ok(content);

            }
            else
            {
                return StatusCode((int)response.StatusCode, "Error fetching stock data: " + response.ReasonPhrase);
            }
        }

        [HttpGet("totalsummary")]
        public async Task<IActionResult> GetPortfolioTotalSummary(string watchlistId)
        {
         
            var finnApi = _configuration["FINN_API"];
            var apiKey = _configuration["FINN_API_KEY"];
            using var httpClient = new HttpClient();

            // var symbols = new[] { "AAPL", "MSFT", "GOOGL", "AMZN" };
            var result = currentStore.GetWatchlistPortfolio(watchlistId);
            string[] symbols = result?.ToArray() ?? Array.Empty<string>();

            // Option B: Traditional if-check
            if (result == null)
            {
                return BadRequest("Portfolio not found.");
            }            

            // 1. Create a collection of tasks (requests start immediately)
            var tasks = symbols.Select(symbol =>
                httpClient.GetAsync($"{finnApi}/api/v1/quote?symbol={symbol}&token={apiKey}")
            );

            // 2. Await all tasks to finish
            var responses = await Task.WhenAll(tasks);
            List<Object> objx = new List<Object>();

            // 3. Process results
            // need to make it friendlier
            foreach (var response in responses)
            {
                var content = await response.Content.ReadAsStringAsync();
                objx.Add(content);
            }

            return Ok(objx);
        }
    }
}
