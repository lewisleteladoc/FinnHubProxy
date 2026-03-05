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
        private readonly WatchListStore watchlistStore;
        private readonly UserWatchlistStore userWatchlistStore;

        // The framework injects the singleton here
        public WatchListController(WatchListStore store,
            UserWatchlistStore userWatchlistStore)
        {
            watchlistStore = store;
            this.userWatchlistStore = userWatchlistStore;
        }

        [HttpPost]
        public IActionResult CreateWatchList([FromBody] WatchListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WatchlistName))
                return BadRequest("Name is required.");

            // Access the singleton via watchlistStore
            var newId = watchlistStore.CreateWatchlist(request.WatchlistName);

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
            watchlistStore.AddToWatchlist(watchlistId, request.Symbol);

            return Ok(new
            {
                Message = $"Symbol '{request.Symbol}' added to Watchlist {watchlistId}.",
                WatchlistId = watchlistId,
                UpdatedAt = DateTime.UtcNow
            });
        }

        [HttpPut("{watchlistId}/bulk")]
        public async Task<IActionResult> BulkAddToWatchlist(string watchlistId, [FromBody] string[] symbols)
        {
            if (symbols == null || symbols.Length == 0)
                return BadRequest("No symbols provided.");
            
            watchlistStore.AddToWatchlist(watchlistId, symbols);            

            return Ok(new
            {
                Message = $"{symbols.Length} symbols added to Watchlist {watchlistId}.",
                WatchlistId = watchlistId,
                SymbolsAdded = symbols, // Return the array to confirm
                UpdatedAt = DateTime.UtcNow
            });
        }


        [HttpGet("{watchlistId}")]
        public IActionResult GetUserWatchListSecurities(string userId, string watchlistId) // Changed from Task<IActionResult>
        {            
            var userHasWatchlist = userWatchlistStore.UserHasWatchlistId(userId, watchlistId);

            // Check if the resource exists
            if (!userHasWatchlist)
            {
                return NotFound(new { Message = $"Watchlist with ID '{watchlistId}' not found." });
            }

            var result = watchlistStore.GetWatchlistPortfolio(watchlistId);

            return Ok(new
            {
                WatchlistId = watchlistId,
                Symbols = result ?? new List<string>(),
                RetrievedAt = DateTime.UtcNow
            });
        }
        
        [HttpGet("{userId}/getuserwatchlists")]
        public IActionResult GetAllOfUserWatchlists(string userId) // Changed from Task<IActionResult>
        {
            // get the watchlist Ids/primary key of a Watchlist table(id, name)
            var watchlistIds = userWatchlistStore.GetUserWatchlists(userId);
            var watchlistFullnames = watchlistStore.GetWatchlistFullName(watchlistIds ?? new List<string>());

            return Ok(new
            {
                username = userId,
                watchlists = watchlistFullnames ?? new List<KeyValuePair<string, string>>(),
                RetrievedAt = DateTime.UtcNow
            });
        }

    }
}
