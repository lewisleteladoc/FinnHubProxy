using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using FinnHubProxy.Services;
using Microsoft.VisualBasic;
using System.Data;

namespace FinnHubProxy.Controllers
{    
    public class WatchListRequest
    {
        public string WatchlistName { get; set; }
    }

    // DTO for adding a symbol
    public record AddSymbolRequest(
        [Required][StringLength(10)] string Symbol
    );

    [ApiController]
    [Route("api/watchlist")]
    public class WatchListController : ControllerBase
    {
        private readonly WatchListStore currentStore;

        // The framework injects the singleton here
        public WatchListController(WatchListStore store)
        {
            currentStore = store;
        }

        [HttpPost]
        public IActionResult CreateWatchList([FromBody] WatchListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WatchlistName))
                return BadRequest("Name is required.");

            // Access the singleton via currentStore
            var newId = currentStore.CreateWatchlist(request.WatchlistName);

            if (newId != "" && newId != null) {
                return Ok(new
                {
                    Id = newId,
                    Message = $"Watchlist '{request.WatchlistName}' created."
                });
            }

            // Return a 409 Conflict if the resource already exists
            return Conflict(new
            {
                message = $"A watchlist with the name '{request.WatchlistName}' already exists."
            });

        }

        /// <summary>
        /// Updates a specific watchlist by adding or replacing a symbol.
        /// </summary>
        /// <param name="watchlistId">The unique ID of the watchlist.</param>
        /// <param name="request">The symbol to add.</param>
        [HttpPut("{watchlistId}")]
        public async Task<IActionResult> AddToWatchlist(string watchlistId, [FromBody] AddSymbolRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Logic to update the watchlist with the new symbol goes here
            // Example: await _context.WatchLists.UpdateSymbolAsync(watchlistId, request.Symbol);
            currentStore.AddToWatchlist(watchlistId, request.Symbol);

            return Ok(new
            {
                Message = $"Symbol '{request.Symbol}' added to Watchlist {watchlistId}.",
                WatchlistId = watchlistId,
                UpdatedAt = DateTime.UtcNow
            });
        }       

        [HttpGet("{watchlistId}")]
        public IActionResult GetWatchList(string watchlistId) // Changed from Task<IActionResult>
        {
            var result = currentStore.GetWatchlistPortfolio(watchlistId);

            // Check if the resource exists
            if (result == null)
            {
                return NotFound(new { Message = $"Watchlist with ID '{watchlistId}' not found." });
            }

            return Ok(new
            {
                WatchlistId = watchlistId,
                Symbols = result ?? new List<string>(),
                RetrievedAt = DateTime.UtcNow
            });
        }
    }
}
