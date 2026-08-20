using System;
using System.Net;

namespace Xst.Rpc
{
    public class XstRpcException : Exception
    {
        public int? Code { get; }

        public string Method { get; }

        public HttpStatusCode? StatusCode { get; }

        public XstRpcException(string message, string method = null, int? code = null,
                               HttpStatusCode? statusCode = null, Exception inner = null)
            : base(message, inner)
        {
            Method = method;
            Code = code;
            StatusCode = statusCode;
        }
    }

    public sealed class XstAuthenticationException : XstRpcException
    {
        public XstAuthenticationException(string message, string method = null)
            : base(message, method, null, HttpStatusCode.Unauthorized)
        {
        }
    }
}
