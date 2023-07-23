using System.Net;
using FluentAssertions;
using HNWebApi.Controllers;
using HNWebApi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using static HNWebApi.Constants;

namespace HNWebApi.Tests;

public class BestStoriesControllerFixture
{
    private readonly string[] _jsonStrings =
    {
        @"{
              ""id"": 0,
              ""by"": ""ismaildonmez"",
              ""score"": 1716,
              ""descendants"": 588
            }",
        @"{
              ""id"": 1,
              ""by"": ""ismaildonmez"",
              ""score"": 1600,
              ""descendants"": 588
            }",
        @"{
              ""id"": 2,
              ""by"": ""ismaildonmez"",
              ""score"": 1500,
              ""descendants"": 588
            }"
    };

    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly BestStoriesController _sut;

    public BestStoriesControllerFixture()
    {
        var logger = Substitute.For<ILogger<BestStoriesController>>();
        _mockHttpHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();

        var services = new ServiceCollection();
        services.AddMemoryCache();
        var serviceProvider = services.BuildServiceProvider();

        var memoryCache = serviceProvider.GetService<IMemoryCache>();
        SetStoryResponse(0);
        SetStoryResponse(1);
        SetStoryResponse(2);
        _sut = new BestStoriesController(logger, memoryCache, () => new HttpClient(_mockHttpHandler));
    }

    private void SetBestStoriesSuccessResponse(string content)
    {
        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                    request.RequestUri.AbsoluteUri == BestStoriesUrl),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }

    private void SetStoryResponse(int id)
    {
        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                    request.RequestUri.AbsoluteUri == string.Format(StoryDetailsUrlFormat, id)),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_jsonStrings[id])
            });
    }

    [Fact]
    public async Task Returns500ErrorWhenBestStoriesIsNotSuccessful()
    {
        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                request.RequestUri.AbsoluteUri == BestStoriesUrl), Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        var actual = await _sut.Get(null);
        var statusCodeResult = actual as StatusCodeResult;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);;
    }

    [Fact]
    public async Task ReturnsOneStory()
    {
        SetBestStoriesSuccessResponse("[0]");

        var actual = await _sut.Get(null);
        var okResult = actual as OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult.Value as IEnumerable<OutputStoryDetails>;
        value.Should().NotBeNull();
        value.Should().ContainSingle();
    }

    [Fact]
    public async Task ReturnsCachedResponses()
    {
        SetBestStoriesSuccessResponse("[0]");

        var actual = await _sut.Get(null);
        var okResult = actual as OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult.Value as IEnumerable<OutputStoryDetails>;
        value.Should().NotBeNull();
        value.Should().ContainSingle();

        var secondCall = await _sut.Get(null);
        var secondOkResult = secondCall as OkObjectResult;
        var value2 = secondOkResult.Value as IEnumerable<OutputStoryDetails>;
        value2.Should().Equal(value);

        _mockHttpHandler.ReceivedWithAnyArgs(2).MockSend(default, default);
    }

    [Fact]
    public async Task ResultsAreSortedByScore()
    {
        SetBestStoriesSuccessResponse("[0,1,2]");

        var actual = await _sut.Get(null);
        var okResult = actual as OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult.Value as IEnumerable<OutputStoryDetails>;
        value.Should().HaveCount(3);
        value.Select(x => x.Score).Should().Equal(1716, 1600, 1500);
    }

    [Fact]
    public async Task AppliesLimit()
    {
        SetBestStoriesSuccessResponse("[0,1,2]");

        var actual = await _sut.Get(2);
        var okResult = actual as OkObjectResult;
        okResult.Should().NotBeNull();
        var value = okResult.Value as IEnumerable<OutputStoryDetails>;
        value.Should().HaveCount(2);
        value.Select(x => x.Score).Should().Equal(1716, 1600);
    }
}