using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;

namespace Xst.Rpc.Tests
{
    internal sealed class FakeDaemon : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _loop;

        public List<JObject> Requests { get; } = new List<JObject>();

        public string LastAuthorizationHeader { get; private set; }

        public string ResponseBody { get; set; } = "{\"result\":null,\"error\":null,\"id\":1}";

        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

        public int Port { get; }

        public FakeDaemon()
        {
            Port = FreePort();
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:" + Port + "/");
            _listener.Start();
            _loop = Task.Run(() => ServeAsync(_cts.Token));
        }

        public XstClientOptions Options(string user = "u", string password = "p")
        {
            return new XstClientOptions
            {
                Host = "127.0.0.1",
                Port = Port,
                Username = user,
                Password = password,
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public XstClient Client(string user = "u", string password = "p")
        {
            return new XstClient(Options(user, password));
        }

        public void RespondWithResult(string resultJson)
        {
            ResponseStatus = HttpStatusCode.OK;
            ResponseBody = "{\"result\":" + resultJson + ",\"error\":null,\"id\":1}";
        }

        public void RespondWithError(int code, string message,
                                     HttpStatusCode status = HttpStatusCode.InternalServerError)
        {
            ResponseStatus = status;
            ResponseBody = "{\"result\":null,\"error\":{\"code\":" + code +
                           ",\"message\":\"" + message + "\"},\"id\":1}";
        }

        private async Task ServeAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                try
                {
                    LastAuthorizationHeader = context.Request.Headers["Authorization"];

                    string body;
                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    {
                        body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        lock (Requests) Requests.Add(JObject.Parse(body));
                    }

                    var payload = Encoding.UTF8.GetBytes(ResponseBody);
                    context.Response.StatusCode = (int)ResponseStatus;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = payload.Length;
                    await context.Response.OutputStream.WriteAsync(payload, 0, payload.Length, token)
                                 .ConfigureAwait(false);
                    context.Response.OutputStream.Close();
                }
                catch
                {
                }
            }
        }

        private static int FreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public JObject LastRequest
        {
            get
            {
                lock (Requests)
                {
                    return Requests.Count == 0 ? null : Requests[Requests.Count - 1];
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
            try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _cts.Dispose();
        }
    }
}
