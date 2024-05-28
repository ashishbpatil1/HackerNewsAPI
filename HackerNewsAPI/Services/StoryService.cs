using System.Text.Json;
using HackerNewsAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsAPI.Services
{
    public class StoryService : IStoryService
    {
        private readonly ILogger<StoryService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public StoryService(ILogger<StoryService> logger, IConfiguration config, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _logger = logger;
            _config = config;
            _httpClient = httpClientFactory.CreateClient("HackerNewsHttpClient");
            _cache = cache;
        }

        /// <summary>
        /// Get top stories
        /// </summary>
        /// <returns>IEnumerable<int></returns>
        public async Task<IEnumerable<int>> GetTopStoriesAsync(int numberOfStories)
        {
            try
            {
                if (!_cache.TryGetValue("TopStories", out List<int> topStories))
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(_config["API:TopStories"]);
                    response.EnsureSuccessStatusCode();
                    string responseData = await response.Content.ReadAsStringAsync();

                    // Deserialize the JSON response to a List<int>
                    topStories = JsonSerializer.Deserialize<List<int>>(responseData);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetSlidingExpiration(TimeSpan.FromMinutes(5)); // Adjust the expiration time as needed

                    _cache.Set("TopStories", topStories, cacheEntryOptions);
                }
                return topStories.Take(numberOfStories);
            }
            catch (HttpRequestException e)
            {
                // Handle HTTP request errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetTopStoriesAsync)}] Error occurred while getting top stories.");
                throw;
            }
            catch (TaskCanceledException e) when (!e.CancellationToken.IsCancellationRequested)
            {
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetTopStoriesAsync)}] Getting top stories request timed out.");
                throw;
            }
            catch (JsonException e)
            {
                // Handle JSON deserialization errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetTopStoriesAsync)}] Error occurred while handling top stories deserialization.");
                throw;
            }
            catch (Exception e)
            {
                // Handle other unexpected errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetTopStoriesAsync)}] Unexpected error occurred while getting top stories.");
                throw;
            }
        }

        /// <summary>
        /// Get story details by id
        /// </summary>
        /// <param name="ids">Story Id's</param>
        /// <returns>List<StoryModel></returns>
        public async Task<List<StoryViewModel>> GetStoryAsync(IEnumerable<int> ids)
        {
            try
            {
                var tasks = ids.Select(id => GetStoryByIdAsync(id)).ToList();
                var results = await Task.WhenAll(tasks);
                return results.ToList();
            }
            catch (Exception e)
            {
                // Handle other unexpected errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetStoryAsync)}] Unexpected error occurred while getting stories.");
                throw;
            }
        }

        #region private methods
        /// <summary>
        /// Get Story by id
        /// </summary>
        /// <param name="id">story id int</param>
        /// <returns>StoryModel</returns>
        private async Task<StoryViewModel> GetStoryByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"Story_{id}";
                if (!_cache.TryGetValue(cacheKey, out StoryViewModel story))
                {
                    var requestUri = _config["API:Story"]?.Replace("#id#", id.ToString());
                    HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode();

                    string responseData = await response.Content.ReadAsStringAsync();
                    story = JsonSerializer.Deserialize<StoryViewModel>(responseData);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                       .SetSlidingExpiration(TimeSpan.FromMinutes(5)); // Adjust the expiration time as needed

                    _cache.Set(cacheKey, story, cacheEntryOptions);
                }
                return story;
            }
            catch (HttpRequestException e)
            {
                // Handle HTTP request errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetStoryByIdAsync)}] Error occurred while getting story.");
                throw;
            }
            catch (TaskCanceledException e) when (!e.CancellationToken.IsCancellationRequested)
            {
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetStoryByIdAsync)}] Getting story request timed out.");
                throw;
            }
            catch (JsonException e)
            {
                // Handle JSON deserialization errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetStoryByIdAsync)}] Error occurred while handling story deserialization.");
                throw;
            }
            catch (Exception e)
            {
                // Handle other unexpected errors
                _logger.LogError(e, $"[{nameof(StoryService)}/{nameof(GetStoryByIdAsync)}] Unexpected error occurred while getting story.");
                throw;
            }
        }
        #endregion
    }
}
