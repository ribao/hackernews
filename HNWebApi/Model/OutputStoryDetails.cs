namespace HNWebApi.Model;

public record OutputStoryDetails(string Title, string Uri, string PostedBy, DateTime Time, int Score, int CommentCount);