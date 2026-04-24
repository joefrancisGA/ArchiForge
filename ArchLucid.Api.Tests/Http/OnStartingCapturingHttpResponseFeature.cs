using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ArchLucid.Api.Tests.Http;

/// <summary>
///     <see cref="DefaultHttpContext" /> does not invoke <see cref="IHttpResponseFeature.OnStarting" /> when the response
///     is written in unit tests.
///     This feature captures callbacks so tests can run them explicitly (same order as Kestrel: last registered runs
///     first).
/// </summary>
public sealed class OnStartingCapturingHttpResponseFeature : IHttpResponseFeature
{
    private readonly Stack<(Func<object, Task> Callback, object State)> _onStarting = new();

    public int StatusCode
    {
        get;
        set;
    }

    public string? ReasonPhrase
    {
        get;
        set;
    }

    public IHeaderDictionary Headers
    {
        get;
        set;
    } = null!;

    public Stream Body
    {
        get;
        set;
    } = null!;

    public bool HasStarted
    {
        get;
        set;
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _onStarting.Push((callback, state));
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);
    }

    /// <summary>
    ///     Runs registered <c>OnStarting</c> callbacks in reverse registration order, matching the host pipeline.
    /// </summary>
    public async Task InvokeOnStartingCallbacksAsync()
    {
        while (_onStarting.Count > 0)
        {
            (Func<object, Task> callback, object state) = _onStarting.Pop();

            await callback(state);
        }
    }

    /// <summary>
    ///     Creates a <see cref="DefaultHttpContext" /> whose sole <see cref="IHttpResponseFeature" /> is this capturing
    ///     implementation.
    ///     Replacing the feature after <c>new DefaultHttpContext()</c> can leave <see cref="HttpResponse" /> bound to the
    ///     original feature,
    ///     so middleware would register <c>OnStarting</c> on one instance while <see cref="HttpResponse.Headers" /> writes to
    ///     another.
    /// </summary>
    public static DefaultHttpContext CreateContext(out OnStartingCapturingHttpResponseFeature responseFeature)
    {
        FeatureCollection features = new();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());

        MemoryStream body = new();
        responseFeature = new OnStartingCapturingHttpResponseFeature
        {
            StatusCode = StatusCodes.Status200OK, Headers = new HeaderDictionary(), Body = body
        };

        features.Set<IHttpResponseFeature>(responseFeature);
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(body));

        return new DefaultHttpContext(features);
    }
}
