using System;

namespace Xst.Rpc
{
    public sealed class XstClientOptions
    {
        public const int DefaultPort = 46502;

        public const int DefaultTestnetPort = 46503;

        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = DefaultPort;

        public string Path { get; set; } = "/";

        public bool UseSsl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public string JsonRpcVersion { get; set; } = "1.0";

        public string UserAgent { get; set; } = "xst-dotnet/0.1";

        internal Uri BuildUri()
        {
            var builder = new UriBuilder
            {
                Scheme = UseSsl ? "https" : "http",
                Host = Host,
                Port = Port,
                Path = string.IsNullOrEmpty(Path) ? "/" : Path
            };
            return builder.Uri;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentException("Host must be set.", nameof(Host));
            if (Port <= 0 || Port > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be 1-65535.");
            if (Timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be positive.");
        }
    }
}
