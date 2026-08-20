using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xst.Rpc.Internal
{
    internal sealed class JsonRpcTransport : IDisposable
    {
        private readonly HttpClient _http;
        private readonly XstClientOptions _options;
        private readonly bool _ownsHttpClient;
        private readonly Uri _endpoint;
        private int _nextId;
        private bool _disposed;

        internal JsonRpcTransport(XstClientOptions options, HttpClient httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _endpoint = _options.BuildUri();

            _ownsHttpClient = httpClient == null;
            _http = httpClient ?? new HttpClient();
            _http.Timeout = _options.Timeout;

            if (!string.IsNullOrEmpty(_options.Username))
            {
                var pair = _options.Username + ":" + (_options.Password ?? string.Empty);
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(pair));
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encoded);
            }

            _http.DefaultRequestHeaders.UserAgent.Clear();
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        }

        internal async Task<JToken> InvokeAsync(string method, object[] parameters,
                                                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Method must be set.", nameof(method));

            var id = Interlocked.Increment(ref _nextId);
            var body = BuildRequest(method, parameters, id);

            string responseText;
            HttpStatusCode status;

            try
            {
                using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
                using (var response = await _http
                    .PostAsync(_endpoint, content, cancellationToken)
                    .ConfigureAwait(false))
                {
                    status = response.StatusCode;
                    responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new XstRpcException(
                    "The daemon did not answer within " + _options.Timeout + ".", method);
            }
            catch (HttpRequestException ex)
            {
                throw new XstRpcException(
                    "Could not reach the Stealth daemon at " + _endpoint + ": " + ex.Message,
                    method, null, null, ex);
            }

            if (status == HttpStatusCode.Unauthorized)
            {
                throw new XstAuthenticationException(
                    "The daemon rejected the RPC credentials. Check rpcuser and rpcpassword " +
                    "in StealthCoin.conf.", method);
            }

            return ReadResult(method, responseText, status);
        }

        private string BuildRequest(string method, object[] parameters, int id)
        {
            var array = new JArray();

            if (parameters != null)
            {
                var last = parameters.Length - 1;
                while (last >= 0 && parameters[last] == null) last--;

                for (var i = 0; i <= last; i++)
                {
                    array.Add(parameters[i] == null ? JValue.CreateNull() : JToken.FromObject(parameters[i]));
                }
            }

            var request = new JObject
            {
                ["jsonrpc"] = _options.JsonRpcVersion,
                ["id"] = id,
                ["method"] = method,
                ["params"] = array
            };

            return request.ToString(Formatting.None);
        }

        private static JToken ReadResult(string method, string responseText, HttpStatusCode status)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new XstRpcException(
                    "The daemon returned an empty body (HTTP " + (int)status + ").", method, null, status);
            }

            JObject envelope;
            try
            {
                envelope = ParseJson(responseText);
            }
            catch (JsonException ex)
            {
                var preview = responseText.Length > 200 ? responseText.Substring(0, 200) + "..." : responseText;
                throw new XstRpcException(
                    "The daemon returned something that is not JSON (HTTP " + (int)status + "): " + preview,
                    method, null, status, ex);
            }

            var error = envelope["error"];
            if (error != null && error.Type != JTokenType.Null)
            {
                var code = error["code"] != null ? (int?)error["code"].Value<int>() : null;
                var message = error["message"] != null
                    ? error["message"].Value<string>()
                    : error.ToString(Formatting.None);

                throw new XstRpcException(
                    "The daemon refused " + method + ": " + message, method, code, status);
            }

            if (status != HttpStatusCode.OK)
            {
                throw new XstRpcException(
                    "The daemon answered HTTP " + (int)status + " for " + method + ".", method, null, status);
            }

            return envelope["result"] ?? JValue.CreateNull();
        }

        internal static JObject ParseJson(string text)
        {
            using (var stringReader = new StringReader(text))
            using (var reader = new JsonTextReader(stringReader)
            {
                FloatParseHandling = FloatParseHandling.Decimal,
                DateParseHandling = DateParseHandling.None
            })
            {
                return JObject.Load(reader);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ownsHttpClient) _http.Dispose();
        }
    }
}
