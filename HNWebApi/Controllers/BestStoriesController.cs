using HNWebApi.Model;
using HNWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HNWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BestStoriesController : ControllerBase
{
    private readonly IStoryRepository _storyRepository;

    public BestStoriesController(IStoryRepository storyRepository)
    {
        _storyRepository = storyRepository;
    }

    [HttpGet(Name = "GetBestStories")]
    public async Task<IEnumerable<OutputStoryDetails>> Get([FromQuery(Name = "limit")] int? limit)
    {
        var responses = await _storyRepository.GetStories();
        if (limit is > 0) responses = responses.Take(limit.Value);

        return responses;
    }
}