namespace FinnHubProxy.Services
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    public class StartupFileLoaderService : BackgroundService
    {
        private readonly ILogger<StartupFileLoaderService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly UserStore userStore;
        private readonly WatchListStore watchListStore;
        private readonly UserWatchlistStore userWatchlistStore;
        private readonly SecuritiesStoriesStore securitiesStoriesStore;

        private readonly Random _random = new Random();

        private string GetRandomValue(List<string> values)
        {
            if (values == null || values.Count == 0)
                throw new ArgumentException("List cannot be null or empty.");

            int index = _random.Next(values.Count);
            return values[index];
        }

        private string[] GetUniqueRandomSubset(string[] originalArray)
        {
            Random rng = new Random();
            float percentage = 0.20f;

            // 1. Get only unique items
            var uniqueItems = originalArray.Distinct().ToArray();

            // 2. Calculate 20% of the UNIQUE items count
            int countToTake = (int)(uniqueItems.Length * percentage);

            // 3. Shuffle and return as an array
            return uniqueItems
                .OrderBy(x => rng.Next())
                .Take(countToTake)
                .ToArray();
        }

        public StartupFileLoaderService(
            ILogger<StartupFileLoaderService> logger,
            IWebHostEnvironment env,
            UserStore userStore,
            WatchListStore currentStore,
            UserWatchlistStore userWatchlistStore,
            SecuritiesStoriesStore securitiesStoriesStore)
        {
            _logger = logger;
            _env = env;
            this.userStore = userStore;
            this.watchListStore = currentStore;
            this.userWatchlistStore = userWatchlistStore;
            this.securitiesStoriesStore = securitiesStoriesStore;   
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StartupFileLoaderService started.");
            var userIds = new List<string>();
            var symbolsList = Array.Empty<string>();
            try
            {
                // Example: read files from MockData folder
                var basePath = Path.Combine(_env.ContentRootPath, "MockData");

                var usersPath = Path.Combine(basePath, "users.txt");
                var symbolsPath = Path.Combine(basePath, "symbols.txt");
                var watchlistNamesPath = Path.Combine(basePath, "watchlistNames.txt");

                if (File.Exists(symbolsPath))
                {
                    symbolsList = await File.ReadAllLinesAsync(symbolsPath, stoppingToken);                 
                }

                if (File.Exists(usersPath))
                {
                    var users = await File.ReadAllLinesAsync(usersPath, stoppingToken);                    

                    foreach (var line in users)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var parts = line.Split(',', StringSplitOptions.TrimEntries);

                        if (parts.Length < 2)
                        {
                            _logger.LogWarning($"Invalid line format: {line}");
                            continue;
                        }

                        var username = parts[0];
                        var guid = parts[1];
                        userIds.Add(username);

                        // Example usage:
                        userStore.AddUser(username, guid);                        
                    }

                }

                if (File.Exists(watchlistNamesPath))
                {
                    var names = await File.ReadAllLinesAsync(watchlistNamesPath, stoppingToken);                    

                    foreach (var watchlistName in names)
                    {
                        if (string.IsNullOrWhiteSpace(watchlistName))
                            continue;


                        // Example usage:
                        // This will create a Collection[watchlistName, watchlistId]
                        // Then it will add to WatchlistSecuritiesStore[watchlistId],List<string>]
                        string watchlistId = watchListStore.CreateWatchlist(watchlistName);
                        userWatchlistStore.AddUserWatchlistRelationship(GetRandomValue(userIds), watchlistId);
                        watchListStore.AddToWatchlist(watchlistId, GetUniqueRandomSubset(symbolsList));
                    }                    
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading startup files.");
            }

            _logger.LogInformation("StartupFileLoaderService finished.");

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
