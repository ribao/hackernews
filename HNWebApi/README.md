# Hacker News .NET API

This is a quick implementation of an API to retrieve the top N comments from Hacker News by using their API

# How to run the project

dotnet run --project HNWebApi/HNWebAPI.csproj

The app will be listening on port 5037.

There is only one endpoint - beststories - which supports and optional query parameter named limit. Below is an example of how to request the top 10 best comments.

http://localhost:5037/beststories?limit=10

Or use Swagger UI:
http://localhost:5037/swagger

## How it works

It uses ASP .NET MemoryCache to cache the HN API responses. Right now it's configured to cache them for 30 minutes but this can be changed on Constants.cs - **ExpirationTime**

When we receive the first request (or its response expires and is evicted from the cache) we get the best stories identifiers and store them in the cache. HN is already returning them in the correct order (score, descending) so if the user has specified a limit **N** we can apply it now to get the details for the top **N** stories.

Then we get the details for each selected story, checking first if it's available in the cache. If not we invoke the HN /item endpoint and store the result in the cache.

Once we have all the stories, we sort them by score in descending order because the previous requests may have finished in a different order.

The responses are mapped to a different DTO because the desired JSON has different property names: commentCount instead of descendants, etc.

{
"title": "A uBlock Origin update was rejected from the Chrome Web Store",
"uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
"postedBy": "ismaildonmez",
"time": "2019-10-12T13:43:01+00:00",
"score": 1716,
"commentCount": 572
}

## Unit tests

There are a few test cases to verify the API is actually using the cache and it is returning results sorted by score in descending order.

## Things to improve

Right now the first user will experience some delay while the API fetches the list of id's and then proceeds to fetch the details for the requested stories. And this delay will happen again once the data expires after 30 minutes.
We could introduce a component that preloads this data when the app starts up and then everyone will get pre-cached responses.
When the data expires, we could fetch the latest version and replace the old one in the background.

With above improvements nobody would have to experience any delay even if requesting the details of all best stories.

This component could be just another class in the existing codebase, or we could split it into two microservices and have them use a distributed cache like Redis.  We don't need this right now because HN gives us just 200 stories and the first request takes less than 2 seconds to complete even if we don't specify a limit.