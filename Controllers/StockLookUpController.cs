using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;

namespace FinnHubProxy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockLookUpController : ControllerBase
    {
        private readonly ILogger<StockLookUpController> _logger;
        private readonly IConfiguration _configuration;
        private const string FINN_API = "";

        public StockLookUpController(ILogger<StockLookUpController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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

            } else {

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

            } else {
                return StatusCode((int)response.StatusCode, "Error fetching stock data: " + response.ReasonPhrase);
            }
        }
    }
}

/*
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:5180"
    },
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "dotnetRunMessages": true,
      "applicationUrl": "https://localhost:7060;http://localhost:5180"
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Container (Dockerfile)": {
      "commandName": "Docker",
      "launchBrowser": true,
      "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}/swagger",
      "environmentVariables": {
        "ASPNETCORE_HTTPS_PORTS": "8081",
        "ASPNETCORE_HTTP_PORTS": "8080"
      },
      "publishAllPorts": true,
      "useSSL": true
    }
  },
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:64953",
      "sslPort": 44338
    }
  }
}
*/