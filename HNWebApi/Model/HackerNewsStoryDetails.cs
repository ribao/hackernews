namespace HNWebApi.Model;

public record HackerNewsStoryDetails
{
    public string Title { get; set; }

    public string By { get; set; }
    public string Url { get; set; }
    public int Descendants { get; set; }
    public int Score { get; set; }
    public long Time { get; set; }
}