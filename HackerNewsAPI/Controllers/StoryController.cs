using HackerNewsAPI.Models;
using HackerNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;

namespace HackerNewsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoryController : ControllerBase
    {
        private readonly ILogger<StoryController> _logger;
        private readonly IConfiguration _config;
        private IStoryService _storyService;
        private readonly int _DEFAULT_STORIES_TO_FETCH = 200;

        public StoryController(ILogger<StoryController> logger, IConfiguration config, IStoryService storyService)
        {
            _logger = logger;
            _config = config;
            _storyService = storyService;
            if (int.TryParse(_config["NumberOfStoriesToFetch"], out int result))
            {
                _DEFAULT_STORIES_TO_FETCH = result;
            }
        }

        /// <summary>
        /// Get top 200 stories
        /// </summary>
        /// <returns>IActionResult</returns>
        [HttpGet]
        [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Client, NoStore = false)]
        public async Task<IActionResult> Get()
        {
            try
            {
                IEnumerable<int> topStories = await _storyService.GetTopStoriesAsync(_DEFAULT_STORIES_TO_FETCH);
                List<StoryViewModel> stories = await _storyService.GetStoryAsync(topStories);
                return Ok(stories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(StoryController)}/{nameof(Get)}] Error occurred while getting top stories.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
