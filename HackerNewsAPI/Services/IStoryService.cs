using HackerNewsAPI.Models;

namespace HackerNewsAPI.Services
{
    public interface IStoryService
    {
        public Task<IEnumerable<int>> GetTopStoriesAsync(int numberOfStories);

        public Task<List<StoryViewModel>> GetStoryAsync(IEnumerable<int> ids);
    }
}
