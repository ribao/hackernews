using FluentAssertions;
using HNWebApi.Controllers;
using HNWebApi.Model;
using HNWebApi.Services;
using NSubstitute;
using Xunit;

namespace HNWebApi.Tests;

public class BestStoriesControllerFixture
{
    private readonly IStoryRepository _storyRepository;

    private readonly BestStoriesController _sut;

    public BestStoriesControllerFixture()
    {
        _storyRepository = Substitute.For<IStoryRepository>();
        _sut = new BestStoriesController(_storyRepository);
    }

    [Fact]
    public async Task ReturnsData()
    {
        var expected = new[]
            { new OutputStoryDetails("title", "url", "postedBy", DateTime.Now, 1, 2) };
        _storyRepository.GetStories().Returns(expected);
        var actual = await _sut.Get(null);
        actual.Should().Equal(expected);
    }

    [Fact]
    public async Task ReturnsDataWithLimit()
    {
        var expected = new[]
        {
            new OutputStoryDetails("title", "url", "postedBy", new DateTime(2023, 1, 1), 100, 2)
        };
        _storyRepository.GetStories().Returns(new[]
        {
            new OutputStoryDetails("title", "url", "postedBy", new DateTime(2023, 1, 1), 100, 2),
            new OutputStoryDetails("title2", "url2", "postedBy", DateTime.Now, 90, 2)
        });
        var actual = await _sut.Get(1);
        actual.Should().Equal(expected);
    }
}