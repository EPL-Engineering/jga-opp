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
        return new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
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

    public async Task WriteAsync(string path, double value)
    {
        string jsonField = JsonConvert.SerializeObject(new { value });   // {"value":0.5}
        var form = new Dictionary<string, string> { { "json", jsonField } };
        using (var content = new FormUrlEncodedContent(form))            // sets x-www-form-urlencoded
        using (var resp = await _http.PostAsync(path, content).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
        }
    }

    // ---- synchronous entry points for MATLAB (can't await a Task) ----
    public T Get<T>(string path) => GetAsync<T>(path).GetAwaiter().GetResult();

    public void Write(string path, double value)        // MATLAB-facing sync wrapper
        => WriteAsync(path, value).GetAwaiter().GetResult();


    public void Dispose() { _http?.Dispose(); }   // "close" — at teardown, not per call
}