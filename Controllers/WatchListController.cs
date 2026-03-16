using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using FinnHubProxy.BusinessObjects;
using FinnHubProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

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
        private readonly ILogger<WatchListController> logger;
        private readonly AdminUserStore adminUserStore;
        private readonly IConfiguration _configuration;
        private readonly TokenService tokenService;
        private readonly List<string> claimStore;

        // The framework injects the singleton here
        public WatchListController(WatchListStore store,
            UserWatchlistStore userWatchlistStore,
            ILogger<WatchListController> logger,
            AdminUserStore adminUserStore,
            IConfiguration _configuration,
            TokenService tokenService)
        {
            this.logger = logger;
            watchlistStore = store;
            this.userWatchlistStore = userWatchlistStore;
            this.adminUserStore = adminUserStore;
            this._configuration = _configuration;
            this.tokenService = tokenService;
            this.claimStore = _configuration["Claims"].Split(",").ToList();
        }

        [HttpPost]
        public IActionResult CreateWatchList([FromBody] WatchListRequest request)
        {
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();

            if (string.IsNullOrWhiteSpace(request.WatchlistName))
                return BadRequest("Name is required.");

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool couldWrite = HasWriteUser(currentUserClaims);
            bool isAdmin = IsAdmin(currentUserClaims);

            if (!couldWrite)
            {
                return Unauthorized(new { Message = "Not authorized." });
            }

            if (isAdmin)
            {
                return StatusCode(403, new { Message = "You do not have permission to perform this action." });
            }
            // Access the singleton via watchlistStore
            var newId = watchlistStore.CreateWatchlist(request.WatchlistName, username ?? "");

            if (newId != "" && newId != null) {
                // Now add a relationship
                userWatchlistStore.AddUserWatchlistRelationship(username, newId);
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

        [HttpDelete()]
        public async Task<IActionResult> DeleteProduct(string watchlistid)
        {
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();
           
            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool couldWrite = HasWriteUser(currentUserClaims);
            bool isAdmin = IsAdmin(currentUserClaims);

            // 1. Find the resource in the database
            var watchlistExists = watchlistStore.WatchlistExists(watchlistid);

            // 2. Return 404 if it doesn't exist
            if (!watchlistExists)
            {
                return NotFound($"Watchlist {watchlistid} does not exists.");
            }

            // 3. Remove the resource and save changes
            watchlistStore.DeleteWatchlist(watchlistid);
            userWatchlistStore.RemoveUserWatchlistRelationship(username, watchlistid, isAdmin);            

            // 4. Return 204 No Content (standard for successful DELETE)
            return NoContent();
        }

        /// <summary>
        /// Updates a specific watchlist by adding or replacing a symbol.
        /// </summary>
        /// <param name="watchlistId">The unique ID of the watchlist.</param>
        /// <param name="request">The symbol to add.</param>
        [HttpPut("{watchlistId}")]        
        public async Task<IActionResult> AddToWatchlist(string watchlistId, [FromBody] AddSymbolRequest request)
        {
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool couldWrite = HasWriteUser(currentUserClaims);
            bool isAdmin = IsAdmin(currentUserClaims);
            bool userHasWatchlist = userWatchlistStore.UserHasWatchlistId(username, watchlistId);

            // Check if the resource exists
            if (!isAdmin && !userHasWatchlist)
            {
                return NotFound(new { Message = $"Watchlist with ID '{watchlistId}' not found." });
            }

            if (!couldWrite)
            {
                return Unauthorized(new { Message = "Not authorized." });
            }

            // Logic to update the watchlist with the new symbol goes here
            // Example: await _context.WatchLists.UpdateSymbolAsync(watchlistId, request.Symbol);
            watchlistStore.AddToWatchlist(watchlistId, request.Symbol);
            logger.LogInformation($"AddToWatchlist {watchlistId} - {request.Symbol}.");

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
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();

            if (symbols == null || symbols.Length == 0)
                return BadRequest("No symbols provided.");

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool couldWrite = HasWriteUser(currentUserClaims);
            bool isAdmin = IsAdmin(currentUserClaims);
            bool userHasWatchlist = userWatchlistStore.UserHasWatchlistId(username, watchlistId);

            // Check if the resource exists
            if (!isAdmin && !userHasWatchlist)
            {
                return NotFound(new { Message = $"Watchlist with ID '{watchlistId}' not found." });
            }

            if (!couldWrite)
            {
                return Unauthorized(new { Message = "Not authorized." });
            }

            watchlistStore.AddToWatchlistInBulk(watchlistId, symbols);            

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
            string username = HttpContext.Items["username"]?.ToString();
            string token = HttpContext.Items["token"]?.ToString();            

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool isAdmin = IsAdmin(currentUserClaims);
            bool userHasWatchlist = userWatchlistStore.UserHasWatchlistId(username, watchlistId);

            // Check if the resource exists
            if (!isAdmin && !userHasWatchlist)
            {
                return NotFound(new { Message = $"Watchlist with ID '{watchlistId}' not found." });
            }
           
            var result = watchlistStore.GetWatchlistPortfolio(watchlistId);
            var watchlist = watchlistStore.GetWatchlistFullName(new List<string> { watchlistId });
            string name = "";

            if (watchlist != null)
            {
                name = watchlist[0].name;
            }

            return Ok(new
            {
                WatchlistId = watchlistId,
                Name = name,
                Symbols = result ?? new List<string>(),
                RetrievedAt = DateTime.UtcNow
            });
        }
        
        [HttpGet("{userId}/getuserwatchlists")]
        public IActionResult GetAllOfUserWatchlists(string userId) // Changed from Task<IActionResult>
        {
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool isAdmin = IsAdmin(currentUserClaims);

            // get the watchlist Ids/primary key of a Watchlist table(id, name)
            var watchlistIds = userWatchlistStore.GetUserWatchlists(username ?? userId, isAdmin);
            var watchlistFullnames = watchlistStore.GetWatchlistFullName(watchlistIds ?? new List<string>());

            return Ok(new
            {
                username = username ?? userId,
                watchlists = watchlistFullnames ?? new List<KeyValuePair<string, string>>(),
                RetrievedAt = DateTime.UtcNow
            });
        }

        [HttpGet("{userId}/getwatchlistwithsymbol")]
        public IActionResult GetWatchlistWithSymbol(string userId, string symbol) // Changed from Task<IActionResult>
        {
            var username = HttpContext.Items["username"]?.ToString();
            var token = HttpContext.Items["token"]?.ToString();

            var currentUserClaims = tokenService.CheckToken(token?.Replace("Bearer ", "") ?? "");

            if (currentUserClaims.Contains("Invalid Token"))
                return Unauthorized(new { Message = "Invalid or expired token." });

            bool isAdmin = IsAdmin(currentUserClaims);

            // get the watchlist Ids/primary key of a Watchlist table(id, name)
            var watchlistIds = userWatchlistStore.GetUserWatchlists(userId, isAdmin);
            var watchlistFullnames = watchlistStore.GetWatchlistWithSymbol(watchlistIds ?? new List<string>(), symbol);
            var watchlistWithSymbol = watchlistStore.GetWatchlistFullName(watchlistFullnames ?? new List<string>());

            return Ok(new
            {
                username = userId,
                security = symbol,
                watchlists = watchlistWithSymbol,
                RetrievedAt = DateTime.UtcNow
            });
        }

        private bool IsAdmin(List<string> currentUserClaims)
        {
            var result = currentUserClaims
                .Where(id => claimStore.Contains(id))
                .ToList();

            return result.Find(t => t == "AdminUser") != null ? true : false;
        }

        private bool HasWriteUser(List<string> currentUserClaims)
        {
            var result = currentUserClaims
                .Where(id => claimStore.Contains(id))
                .ToList();

            return result.Find(t => t == "WriteUser") != null ? true : false;
        }
    }
}
