using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
    [Route("api/[controller]")]
    public class WatchListController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateWatchList([FromBody] WatchListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WatchlistName))
                return BadRequest("Name is required.");

            return Ok(new {
                Id = 20,
                Message = $"Watchlist '{request.WatchlistName}' created."
            }); 
        }

        /// <summary>
        /// Updates a specific watchlist by adding or replacing a symbol.
        /// </summary>
        /// <param name="watchlistId">The unique ID of the watchlist.</param>
        /// <param name="request">The symbol to add.</param>
        [HttpPut("{watchlistId}")]
        public async Task<IActionResult> UpdateWatchList(int watchlistId, [FromBody] AddSymbolRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Logic to update the watchlist with the new symbol goes here
            // Example: await _context.WatchLists.UpdateSymbolAsync(watchlistId, request.Symbol);

            return Ok(new
            {
                Message = $"Symbol '{request.Symbol}' added to Watchlist {watchlistId}.",
                WatchlistId = watchlistId,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
