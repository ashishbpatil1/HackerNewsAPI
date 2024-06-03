using System.Net;
using System.Net.Http;
using System.Text.Json;
using HackerNewsAPI.Models;
using HackerNewsAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HackerNewsAPITest
{
    [TestClass]
    public class StoryServiceTests
    {
        private Mock<ILogger<StoryService>> _loggerMock;
        private Mock<IConfiguration> _configMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<IMemoryCache> _cacheMock;
        private StoryService _storyService;
        private HttpClient _httpClient;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<StoryService>>();
            _configMock = new Mock<IConfiguration>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cacheMock = new Mock<IMemoryCache>();

            _configMock.Setup(config => config["API:TopStories"]).Returns("https://hacker-news.firebaseio.com/v0/topstories.json?print=pretty");
            _configMock.Setup(config => config["API:Story"]).Returns("https://hacker-news.firebaseio.com/v0/item/#id#.json?print=pretty");

            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(_httpClient);

            _storyService = new StoryService(
                _loggerMock.Object,
                _configMock.Object,
                _httpClientFactoryMock.Object,
                _cacheMock.Object
            );
        }

        [TestMethod]
        public async Task GetTopStoriesAsync_ShouldReturnTopStories_FromCache()
        {
            // Arrange
            var cachedStories = new List<int> { 1, 2, 3 };
            object cacheEntry = cachedStories;
            _cacheMock.Setup(c => c.TryGetValue("TopStories", out cacheEntry)).Returns(true);

            // Act
            var result = await _storyService.GetTopStoriesAsync(3);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            CollectionAssert.AreEqual(cachedStories, result.ToList());
        }

        [TestMethod]
        public async Task GetTopStoriesAsync_ShouldReturnTopStories()
        {
            // Arrange
            object cacheEntry = null;
            _cacheMock.Setup(c => c.TryGetValue("TopStories", out cacheEntry)).Returns(false);
            _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>);
            var topStoriesJson = JsonSerializer.Serialize(Enumerable.Range(1, 300).ToList());
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(topStoriesJson)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _storyService.GetTopStoriesAsync(200);

            // Assert
            Assert.AreEqual(200, result.Count());
            CollectionAssert.AreEqual(Enumerable.Range(1, 200).ToList(), result.ToList());
        }

        [TestMethod]
        public async Task GetTopStoriesAsync_ShouldThrowHttpRequestException_OnHttpError()
        {
            // Arrange
            var httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _storyService.GetTopStoriesAsync(200));
        }

        [TestMethod]
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
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<JsonException>(() => _storyService.GetTopStoriesAsync(200));
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
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("https://hacker-news.firebaseio.com/v0/item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var cachedStories = new Dictionary<string, StoryViewModel>
            {
                { "Story_1", new StoryViewModel { id = 1 } },
                { "Story_2", new StoryViewModel { id = 2 } },
                { "Story_3", new StoryViewModel { id = 3 } }
            };
            object cacheEntry = null;
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns((object key, out object value) =>
                {
                    value = cachedStories.GetValueOrDefault((string)key);
                    return value != null;
                });

            // Act
            var result = await _storyService.GetStoryAsync(ids);

            // Assert
            Assert.AreEqual(3, result.Count);
            int i = 0;
            foreach (var story in result)
            {
                Assert.AreEqual(ids[i], story.id);
                i++;
            }
        }


        [TestMethod]
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

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => _storyService.GetStoryAsync(new List<int> { 1 }));
        }

        [TestMethod]
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
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("https://hacker-news.firebaseio.com/v0/item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<JsonException>(() => _storyService.GetStoryAsync(new List<int> { 1 }));
        }
    }
}
