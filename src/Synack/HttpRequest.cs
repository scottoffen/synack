namespace Synack;

public class HttpRequest : IHttpRequest
{
    public Stream Body => throw new NotImplementedException();

    public IReadOnlyDictionary<string, string> Cookies => throw new NotImplementedException();

    public long? ContentLength => throw new NotImplementedException();

    public string? ContentType => throw new NotImplementedException();

    public IReadOnlyDictionary<string, string[]> Headers => throw new NotImplementedException();

    public bool HasBody => throw new NotImplementedException();

    public bool IsSecure => throw new NotImplementedException();

    public string Method => throw new NotImplementedException();

    public string Path => throw new NotImplementedException();

    public string Protocol => throw new NotImplementedException();

    public string RawTarget => throw new NotImplementedException();

    public string QueryString => throw new NotImplementedException();

    public IReadOnlyDictionary<string, string[]> Query => throw new NotImplementedException();

    public Uri Url => throw new NotImplementedException();
}
