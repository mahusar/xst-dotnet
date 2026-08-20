using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc.Internal;
using Xst.Rpc.Models;

namespace Xst.Rpc
{
    public sealed partial class XstClient : IDisposable
    {
        private readonly JsonRpcTransport _transport;
        private bool _disposed;

        public XstClientOptions Options { get; }

        public XstClient(XstClientOptions options, HttpClient httpClient = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _transport = new JsonRpcTransport(options, httpClient);
        }

        public XstClient(string host, int port, string username, string password)
            : this(new XstClientOptions
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password
            })
        {
        }

        public Task<JToken> InvokeAsync(string method, params object[] parameters)
        {
            return _transport.InvokeAsync(method, parameters, CancellationToken.None);
        }

        public Task<JToken> InvokeAsync(string method, object[] parameters,
                                        CancellationToken cancellationToken)
        {
            return _transport.InvokeAsync(method, parameters, cancellationToken);
        }

        private async Task<T> CallAsync<T>(string method, object[] parameters,
                                           CancellationToken cancellationToken)
        {
            var result = await _transport.InvokeAsync(method, parameters, cancellationToken)
                                         .ConfigureAwait(false);

            if (result == null || result.Type == JTokenType.Null) return default;
            return result.ToObject<T>();
        }

        private Task CallVoidAsync(string method, object[] parameters,
                                   CancellationToken cancellationToken)
        {
            return _transport.InvokeAsync(method, parameters, cancellationToken);
        }

        public Task<XstInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<XstInfo>("getinfo", null, cancellationToken);
        }

        public Task<int> GetBlockCountAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<int>("getblockcount", null, cancellationToken);
        }

        public Task<string> GetBestBlockHashAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<string>("getbestblockhash", null, cancellationToken);
        }

        public Task<int> GetConnectionCountAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<int>("getconnectioncount", null, cancellationToken);
        }

        public Task<long> GetAdjustedTimeAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<long>("getadjustedtime", null, cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _transport.Dispose();
        }
    }
}
