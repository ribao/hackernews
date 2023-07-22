using System.Net;
using FluentAssertions;
using HNWebApi.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Xunit;
using static HNWebApi.Constants;

namespace HNWebApi.Tests;

public class StoryRepositoryFixture
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
    private readonly TestScheduler _scheduler = new();
    private readonly ILogger<StoryRepository> _logger;
    private readonly ISchedulerProvider _schedulerProvider;

    public StoryRepositoryFixture()
    {
         _logger = Substitute.For<ILogger<StoryRepository>>();
        _mockHttpHandler = Substitute.ForPartsOf<MockHttpMessageHandler>();
        _schedulerProvider = Substitute.For<ISchedulerProvider>();
        _schedulerProvider.TaskPoolScheduler.Returns(_scheduler);
        SetBestStoriesSuccessResponse("[0,1,2]");
        SetStoryResponse(0);
        SetStoryResponse(1);
        SetStoryResponse(2);
    }

    private StoryRepository CreateSut()
    {
        return new StoryRepository(() => new HttpClient(_mockHttpHandler), _logger, _schedulerProvider);
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
    public void MustWaitUntilReady()
    {
        using var sut = CreateSut();
        var waitTask = sut.WaitUntilReady();
        waitTask.IsCompleted.Should().BeFalse();

        _scheduler.AdvanceBy(1);

        waitTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void MustRefreshAfterExpirationTime()
    {
        using var sut = CreateSut();
        var waitTask = sut.WaitUntilReady();
        waitTask.IsCompleted.Should().BeFalse();

        _scheduler.AdvanceBy(1);

        waitTask.IsCompleted.Should().BeTrue();
        _mockHttpHandler.Received(1).MockSend(Arg.Is<HttpRequestMessage>(request =>
                request.RequestUri.AbsoluteUri == BestStoriesUrl),
            Arg.Any<CancellationToken>());

        _mockHttpHandler.ClearSubstitute();
        SetBestStoriesSuccessResponse("[0,1,2]");
        SetStoryResponse(0);
        SetStoryResponse(1);
        SetStoryResponse(2);
        _scheduler.AdvanceBy(ExpirationTime.Ticks);

        _mockHttpHandler.Received(1).MockSend(Arg.Is<HttpRequestMessage>(request =>
                request.RequestUri.AbsoluteUri == BestStoriesUrl),
            Arg.Any<CancellationToken>());
        _mockHttpHandler.ReceivedWithAnyArgs(4).MockSend(default, default);
    }

    [Fact]
    public async Task ResultsAreSortedByScore()
    {
        using var sut = CreateSut();
        var actual = await sut.GetStories();
        actual.Should().HaveCount(3);
        actual.Select(x => x.Score).Should().Equal(1716, 1600, 1500);
    }

    [Fact]
    public async Task ReturnsSingleResult()
    {
        _mockHttpHandler.ClearSubstitute();
        SetBestStoriesSuccessResponse("[0]");
        SetStoryResponse(0);
        using var sut = CreateSut();
        var actual = await sut.GetStories();
        actual.Should().ContainSingle();
    }

    [Fact]
    public async Task ReturnsEmptyWhenBestStoriesIsNotSuccessful()
    {
        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                request.RequestUri.AbsoluteUri == BestStoriesUrl), Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        using var sut = CreateSut();
        var actual = await sut.GetStories();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsEmptyWhenBestStoriesThrowsException()
    {
        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                request.RequestUri.AbsoluteUri == BestStoriesUrl), Arg.Any<CancellationToken>())
            .Returns(_ => throw new Exception("This a test"));
        using var sut = CreateSut();
        var actual = await sut.GetStories();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task FiltersOutResponsesWithErrors()
    {
        _mockHttpHandler.ClearSubstitute();
        SetBestStoriesSuccessResponse("[0,1,2]");
        SetStoryResponse(0);
        SetStoryResponse(2);

        _mockHttpHandler.MockSend(Arg.Is<HttpRequestMessage>(request =>
                    request.RequestUri.AbsoluteUri == string.Format(StoryDetailsUrlFormat, 1)),
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new Exception("This a test"));
        using var sut = CreateSut();
        var actual = await sut.GetStories();
        actual.Should().HaveCount(2);
    }
}