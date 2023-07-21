namespace  HNWebApi;

public static class Constants
{
    public static readonly TimeSpan ExpirationTime = TimeSpan.FromMinutes(30);
    public const string BestStoriesUrl = "https://hacker-news.firebaseio.com/v0/beststories.json";
    public const string StoryDetailsUrlFormat = "https://hacker-news.firebaseio.com/v0/item/{0}.json";
    public const string BestStoriesResponseKey = "BEST_STORIES_RESPONSE";
}

