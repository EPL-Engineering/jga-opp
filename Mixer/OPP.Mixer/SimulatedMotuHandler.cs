using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

/// <summary>
/// Dev-machine stand-in for the MOTU's HTTP datastore. Wired in automatically by
/// MotuClient when constructed with MotuClient.SimulatedUrl (or "dummy"/"sim").
///
/// Not the same job as FakeMotuHandler: that one captures outgoing requests so unit
/// tests can assert "the library sent POST .../fader with 0.8". This one instead
/// *behaves* like a datastore — it remembers what's written and gives it back on
/// read — so the Mixer UI has something plausible to drive against with no hardware
/// attached. Values not yet written return a sensible default (unity fader, unmuted)
/// rather than throwing, since Mixer.ReadMotuState() queries every channel strip on
/// startup and a missing default there would surface as -Infinity dB.
/// </summary>
public sealed class SimulatedMotuHandler : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, double> _values =
        new ConcurrentDictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri.AbsolutePath.Trim('/');

        if (request.Method == HttpMethod.Get)
            return HandleGet(path);

        if (request.Method == HttpMethod.Post)
            return await HandlePostAsync(path, request, cancellationToken).ConfigureAwait(false);

        return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
    }

    private HttpResponseMessage HandleGet(string path)
    {
        double value = _values.GetOrAdd(path, DefaultFor);
        return JsonResponse($"{{\"value\":{value.ToString(CultureInfo.InvariantCulture)}}}");
    }

    private async Task<HttpResponseMessage> HandlePostAsync(string path, HttpRequestMessage request, CancellationToken ct)
    {
        // MotuClient.PostValueAsync sends form-encoded: json=<url-encoded JSON>
        string body = request.Content == null
            ? string.Empty
            : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

        string jsonField = ParseFormField(body, "json");
        if (jsonField != null)
        {
            var payload = JObject.Parse(jsonField);
            if (payload.TryGetValue("value", out var token))
                _values[path] = token.Value<double>();
        }

        return JsonResponse("{}");
    }

    // Unwritten channels should look "normal", not empty: unity gain (0 dB) and unmuted.
    // TestConnection() only checks the datastore root returns 200, so it doesn't need
    // a specific value here.
    private static double DefaultFor(string path) =>
        path.EndsWith("matrix/fader", StringComparison.OrdinalIgnoreCase) ? 1.0
      : path.EndsWith("matrix/mute", StringComparison.OrdinalIgnoreCase) ? 0.0
      : 0.0;

    private static string ParseFormField(string formBody, string key)
    {
        if (string.IsNullOrEmpty(formBody)) return null;

        foreach (var pair in formBody.Split('&'))
        {
            var kv = pair.Split(new[] { '=' }, 2);
            if (kv.Length == 2 && Uri.UnescapeDataString(kv[0]) == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
