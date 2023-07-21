namespace HNWebApi.Model;

public record OutputStoryDetails
{
    public string Title { get; }
    public string Uri { get; }
    public string PostedBy { get;}
    public DateTime Time { get;  }
    public int Score { get; }
    public int CommentCount { get; }

    public OutputStoryDetails(HackerNewsStoryDetails storyDetails)
    {
        Title = storyDetails.Title;
        PostedBy = storyDetails.By;
        Uri = storyDetails.Url;
        CommentCount = storyDetails.Descendants;
        Score = storyDetails.Score;
        Time = DateTimeOffset.FromUnixTimeSeconds(storyDetails.Time).LocalDateTime;
    }

}