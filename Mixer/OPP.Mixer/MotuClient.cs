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
    // Recognized dev-machine stand-ins for a real MOTU URL. Case-insensitive.
    // Prefer SimulatedUrl in code/config so the literal only lives in one place.
    public const string SimulatedUrl = "simulated";
    private static readonly string[] SimulationSentinels = { SimulatedUrl, "dummy", "sim" };

    private readonly HttpClient _http;
    private readonly string _openedWith;   // raw string passed to the string ctor, incl. sentinels

    // "open" — once, in the controller. Reuse for the client's whole life.
    public MotuClient(string baseUrl) : this(BuildTransport(baseUrl))
    {
        _openedWith = baseUrl;
    }

    // injectable transport — keeps the fake-handler tests working unchanged
    public MotuClient(HttpClient http) { _http = http; }

    public static bool IsSimulatedUrl(string url) =>
        url != null && Array.Exists(SimulationSentinels, s => string.Equals(s, url, StringComparison.OrdinalIgnoreCase));

    private static HttpClient BuildTransport(string baseUrl)
    {
        if (IsSimulatedUrl(baseUrl))
        {
            // No real instrument on this machine — hand back a stateful stand-in instead.
            return new HttpClient(new SimulatedMotuHandler())
            {
                BaseAddress = new Uri("http://simulated.motu.local/")
            };
        }
        return MakeClient(baseUrl);
    }

    private static HttpClient MakeClient(string baseUrl)
    {
        var handler = new HttpClientHandler { UseProxy = false };   // never proxy a local instrument
        var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.ExpectContinue = false;   // don't negotiate 100-continue
        client.DefaultRequestHeaders.ConnectionClose = true;   // Connection: close, no reuse
        return client;
    }

    // Compare against the string this client was opened with (real URL or sentinel),
    // not BaseAddress — a simulated client's BaseAddress is a synthetic placeholder,
    // so comparing raw input is what lets Mixer.IsAvailable("simulated") reuse the
    // same simulated instance (and its accumulated fader/mute state) on repeat calls.
    public bool IsUrl(string url) =>
        _openedWith != null
            ? string.Equals(_openedWith, url, StringComparison.OrdinalIgnoreCase)
            : _http.BaseAddress.ToString().Equals(url, StringComparison.OrdinalIgnoreCase);
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