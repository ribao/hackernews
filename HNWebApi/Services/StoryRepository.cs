using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using HNWebApi.Model;

namespace HNWebApi.Services;

public class StoryRepository : IStoryRepository, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IObservable<IEnumerable<OutputStoryDetails>> _observable;
    private readonly ILogger<StoryRepository> _logger;
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();


    public StoryRepository(Func<HttpClient> httpClientFactory, ILogger<StoryRepository> logger,
        ISchedulerProvider schedulerProvider)
    {
        _logger = logger;
        _schedulerProvider = schedulerProvider;
        _httpClient = httpClientFactory();

        _observable = Observable.Interval(Constants.ExpirationTime, schedulerProvider.TaskPoolScheduler)
            .StartWith(0)
            .Do(_ => logger.LogInformation("Refreshing cache..."))
            .Select(_ => GetBestStories())
            .Switch()
            .Do(x => logger.LogInformation("Got {Count} stories", x.Count()))
            .Select(GetAllStoryDetails)
            .Switch()
            .Do(_ => logger.LogInformation("GetAllStoryDetails requests completed"))
            .Select(x => x.Where(y => y != null).OrderByDescending(y => y.Score))
            .Do(_ => logger.LogInformation("All story details updated"))
            .Replay(1)
            .RefCount();

        _disposables.Add(_observable.Subscribe());
        _disposables.Add(_httpClient);
    }

    public Task WaitUntilReady()
    {
        return _observable.ObserveOn(_schedulerProvider.TaskPoolScheduler).Take(1).ToTask();
    }

    public async Task<IEnumerable<OutputStoryDetails>> GetStories()
    {
        return await _observable.FirstAsync();
    }

    private IObservable<IEnumerable<int>> GetBestStories()
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                var bestStoriesResponse = await _httpClient.GetAsync(Constants.BestStoriesUrl);
                if (!bestStoriesResponse.IsSuccessStatusCode) return Array.Empty<int>();

                return await bestStoriesResponse.Content.ReadFromJsonAsync<IEnumerable<int>>() ?? Array.Empty<int>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error when getting best stories");
                return Array.Empty<int>();
            }

        });
    }

    private IObservable<IEnumerable<OutputStoryDetails>> GetAllStoryDetails(IEnumerable<int> ids)
    {
        return ids.Any() ? ids.Select(GetStoryDetailsObs).Zip() : Observable.Return(Array.Empty<OutputStoryDetails>());
    }

    private IObservable<OutputStoryDetails> GetStoryDetailsObs(int id)
    {
        return Observable.FromAsync(async () =>
        {
            try
            {
                var storyResponse = await _httpClient.GetAsync(string.Format(Constants.StoryDetailsUrlFormat, id));
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

        });
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}