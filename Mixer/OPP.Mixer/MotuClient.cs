using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class MotuClient : IDisposable
{
    private readonly HttpClient _http;

    // "open" — once, in the controller. Reuse for the client's whole life.
    public MotuClient(string baseUrl) : this(MakeClient(baseUrl)) { }

    // injectable transport — keeps the fake-handler tests working unchanged
    public MotuClient(HttpClient http) { _http = http; }

    private static HttpClient MakeClient(string baseUrl)
    {
        var handler = new HttpClientHandler { UseProxy = false };   // never proxy a local instrument
        var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.ExpectContinue = false;   // don't negotiate 100-continue
        client.DefaultRequestHeaders.ConnectionClose = true;   // Connection: close, no reuse
        return client;
    }

    public Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken)
    {
        return _http.GetAsync(path, cancellationToken);
    }

    // GET + deserialize into a typed object
    public async Task<T> GetAsync<T>(string path)
    {
        using (var resp = await _http.GetAsync(path).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    // GET into a loosely-shaped JObject — handy for the datastore's varied responses
    public async Task<JObject> GetJObjectAsync(string path)
    {
        using (var resp = await _http.GetAsync(path).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JObject.Parse(json);
        }
    }

    private async Task PostValueAsync(string path, string jsonField)
    {
        var form = new Dictionary<string, string> { { "json", jsonField } };
        using (var content = new FormUrlEncodedContent(form))
        using (var resp = await _http.PostAsync(path, content).ConfigureAwait(false))
            resp.EnsureSuccessStatusCode();
    }

    public Task WriteAsync(string path, double value)
        => PostValueAsync(path, JsonConvert.SerializeObject(new { value }));   // {"value":0.5}

    public Task WriteAsync(string path, int value)
        => PostValueAsync(path, JsonConvert.SerializeObject(new { value }));   // {"value":0}

    public void Write(string path, double value) => WriteAsync(path, value).GetAwaiter().GetResult();
    public void Write(string path, int value) => WriteAsync(path, value).GetAwaiter().GetResult();
    // ---- synchronous entry points for MATLAB (can't await a Task) ----
    public T Get<T>(string path) => GetAsync<T>(path).GetAwaiter().GetResult();

    //public void Write(string path, double value)        // MATLAB-facing sync wrapper
    //    => WriteAsync(path, value).GetAwaiter().GetResult();


    public void Dispose() { _http?.Dispose(); }   // "close" — at teardown, not per call
}