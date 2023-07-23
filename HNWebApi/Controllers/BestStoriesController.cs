using HNWebApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using static HNWebApi.Constants;

namespace HNWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BestStoriesController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BestStoriesController> _logger;

    public BestStoriesController(ILogger<BestStoriesController> logger, IMemoryCache cache,
        Func<HttpClient> httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClient = httpClientFactory();
    }

    [HttpGet(Name = "GetBestStories")]
    public async Task<IActionResult> Get([FromQuery(Name = "limit")] int? limit)
    {
        try
        {
            var storyList = await _cache.GetOrCreateAsync(BestStoriesResponseKey,
                async cacheEntry => await GetBestStories(cacheEntry));

            if (limit is > 0) storyList = storyList.Take(limit.Value);

            var tasks = storyList.Select(id =>
                _cache.GetOrCreateAsync<OutputStoryDetails>(id, async cacheEntry => await GetStoryDetails(id, cacheEntry)));

            var responses = await Task.WhenAll(tasks);
            return Ok(responses.Where(x => x != null).OrderByDescending(x => x.Score));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error when getting best stories");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<IEnumerable<int>> GetBestStories(ICacheEntry cacheEntry)
    {
        try
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = ExpirationTime;
            _logger.LogInformation("Getting all best stories...");
            var bestStoriesResponse = await _httpClient.GetAsync(BestStoriesUrl);
            if (!bestStoriesResponse.IsSuccessStatusCode)
                throw new Exception($"Could not get best stories. Response code: {bestStoriesResponse.StatusCode}");

            return await bestStoriesResponse.Content.ReadFromJsonAsync<IEnumerable<int>>() ?? Array.Empty<int>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error when getting best stories");
            throw;
        }
    }

    private async Task<OutputStoryDetails> GetStoryDetails(int id, ICacheEntry cacheEntry)
    {
        try
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = ExpirationTime;
            _logger.LogInformation("Getting details for story {Id}...", id);
            var storyResponse = await _httpClient.GetAsync(string.Format(StoryDetailsUrlFormat, id));
            if (!storyResponse.IsSuccessStatusCode) return null;
            var storyDetails = await storyResponse.Content.ReadFromJsonAsync<HackerNewsStoryDetails>();
            return storyDetails == null
                ? null
                : new OutputStoryDetails(storyDetails.Title, storyDetails.Url, storyDetails.By,
                    DateTimeOffset.FromUnixTimeSeconds(storyDetails.Time).LocalDateTime, storyDetails.Score,
                    storyDetails.Descendants);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error when processing story id {Id}", id);
            return null;
        }
    }
}