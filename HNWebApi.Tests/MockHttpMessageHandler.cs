namespace HNWebApi.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(MockSend(request, cancellationToken));

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => MockSend(request, cancellationToken);

    public virtual HttpResponseMessage MockSend(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotImplementedException();
}