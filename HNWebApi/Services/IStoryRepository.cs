using HNWebApi.Model;

namespace HNWebApi.Services;

public interface IStoryRepository
{
    Task WaitUntilReady();
    Task<IEnumerable<OutputStoryDetails>> GetStories();
}