

namespace Synack;

public class HttpResponse : IHttpResponse
{
    public Stream Body => throw new NotImplementedException();

    public IDictionary<string, string[]> Headers => throw new NotImplementedException();

    public bool HasStarted => throw new NotImplementedException();

    public string? ReasonPhrase { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Abort()
    {
        throw new NotImplementedException();
    }

    public void Redirect(string url)
    {
        throw new NotImplementedException();
    }

    public void StartStreaming()
    {
        throw new NotImplementedException();
    }
}
