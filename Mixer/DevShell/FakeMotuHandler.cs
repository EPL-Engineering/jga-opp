using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public sealed class FakeMotuHandler : HttpMessageHandler
{
    private readonly Dictionary<string, object> _store = new Dictionary<string, object>();

    // snapshots captured at send time — survives HttpClient disposing the request
    public List<CapturedRequest> Requests { get; } = new List<CapturedRequest>();

    public void Set(string path, object value) => _store[path.TrimStart('/')] = value;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage req, CancellationToken ct)
    {
        string path = req.RequestUri.AbsolutePath.TrimStart('/');

        string body = null;
        if (req.Content != null)
            body = await req.Content.ReadAsStringAsync().ConfigureAwait(false);  // no ct overload on 4.8

        Requests.Add(new CapturedRequest(req.Method, path, body));

        if (req.Method == HttpMethod.Get)
        {
            object v;
            if (_store.TryGetValue(path, out v))
                return Json("{\"value\":" + JsonConvert.SerializeObject(v) + "}");
        }
        else
        {
            // a write — parse `body` however YOUR client sends sets, then:
            // _store[path] = parsedValue;
        }

        return Json("{}");
    }

    private static HttpResponseMessage Json(string s)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(s, Encoding.UTF8, "application/json")
        };
    }
}

public sealed class CapturedRequest
{
    public HttpMethod Method { get; }
    public string Path { get; }
    public string Body { get; }

    public CapturedRequest(HttpMethod method, string path, string body)
    {
        Method = method;
        Path = path;
        Body = body;
    }
}