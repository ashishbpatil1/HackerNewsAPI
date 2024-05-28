using System.Net;
using System.Text.Json;
using HackerNewsAPI.Models;
using HackerNewsAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace HackerNewsAPITest
{
    [TestClass]
    public class StoryServiceTests
    {
        private Mock<ILogger<StoryService>> _loggerMock;
        private Mock<IConfiguration> _configMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private StoryService _storyService;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<StoryService>>();
            _configMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            _configMock.Setup(config => config["API:TopStories"]).Returns("https://hacker-news.firebaseio.com/v0/topstories.json?print=pretty");
            _configMock.Setup(config => config["API:Story"]).Returns("https://hacker-news.firebaseio.com/v0/item/#id#.json?print=pretty");

            _storyService = new StoryService(_loggerMock.Object, _configMock.Object, _httpClient);
        }

        [TestMethod]
        public async Task GetTopStoriesAsync_ShouldReturnTopStories()
        {
            // Arrange
            var topStoriesJson = JsonSerializer.Serialize(Enumerable.Range(1, 300).ToList());
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(topStoriesJson)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "GetAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _storyService.GetTopStoriesAsync(200);

            // Assert
            Assert.AreEqual(200, result.Count());
            Assert.IsTrue(result.SequenceEqual(Enumerable.Range(1, 200)));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task GetTopStoriesAsync_ShouldThrowHttpRequestException_OnHttpError()
        {
            // Arrange
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "GetAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _storyService.GetTopStoriesAsync(200);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public async Task GetTopStoriesAsync_ShouldThrowJsonException_OnInvalidJson()
        {
            // Arrange
            var invalidJson = "invalid json";
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(invalidJson)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "GetAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _storyService.GetTopStoriesAsync(200);
        }

        [TestMethod]
        public async Task GetStoryAsync_ShouldReturnStories()
        {
            // Arrange
            var ids = new List<int> { 1, 2, 3 };
            var storyJson = JsonSerializer.Serialize(new StoryViewModel { id = 1 });
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(storyJson)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "GetAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("https://hacker-news.firebaseio.com/v0/item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _storyService.GetStoryAsync(ids);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.All(story => story.id == 1));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task GetStoryByIdAsync_ShouldThrowHttpRequestException_OnHttpError()
        {
            // Arrange
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("https://hacker-news.firebaseio.com/v0/item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _storyService.GetStoryAsync(new List<int> { 1 });
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public async Task GetStoryByIdAsync_ShouldThrowJsonException_OnInvalidJson()
        {
            // Arrange
            var invalidJson = "invalid json";
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(invalidJson)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "GetAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("https://hacker-news.firebaseio.com/v0/item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            await _storyService.GetStoryAsync(new List<int> { 1 });
        }
    }
}