namespace HNWebApi.Model;

public record HackerNewsStoryDetails(string Title, string By, string Url, int Descendants, int Score, long Time);