using System.Net;
using System.Security.Claims;

namespace Synack;

public class HttpContext : IHttpContext
{
    protected readonly List<Func<Task>> CompletionHandlers = [];

    public IHttpRequest Request { get; internal set; }

    public IHttpResponse Response { get; internal set; }

    public EndPoint RemoteEndPoint { get; internal set; }

    public EndPoint LocalEndPoint { get; internal set; }

    public ClaimsPrincipal User { get; set; } = new ClaimsPrincipal(new ClaimsIdentity());

    public string TraceIdentifier { get; internal set; }

    public CancellationToken RequestAborted { get; internal set; }

    public virtual void OnCompleted(Func<Task> callback) => CompletionHandlers.Add(callback);

    public virtual async Task ExecuteOnCompletedAsync()
    {
        foreach (var cb in Enumerable.Reverse(CompletionHandlers))
        {
            try { await cb(); } catch { /* swallow exceptions */ }
        }
    }

    public HttpContext(
        IHttpRequest request,
        IHttpResponse response,
        EndPoint remoteEndPoint,
        EndPoint localEndPoint,
        string traceIdentifier,
        CancellationToken requestAborted
    )
    {
        Request = request;
        Response = response;
        RemoteEndPoint = remoteEndPoint;
        LocalEndPoint = localEndPoint;
        TraceIdentifier = traceIdentifier;
        RequestAborted = requestAborted;
    }
}
