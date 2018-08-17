#if NET47
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Text;
    using System.Net;
    using System.Collections.Specialized;
    using System.IO;

    public class HttpRequest : IHttpRequest
    {
        private readonly HttpListenerContext _context;
        
        public HttpRequest(HttpListenerContext context)
        {
            _context = context;
        }

        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }
        public bool KeepAlive { get; }

        public CookieCollection Cookies { get; }
        public string RawUrl { get; }
        public NameValueCollection QueryString { get; }
        public string HttpMethod { get; }
        public Uri Url { get;  }
        public bool HasEntityBody { get; }
        public Stream InputStream { get; }

        public Encoding ContentEncoding { get; }
        public IPEndPoint RemoteEndPoint { get; }
        public bool IsLocal => _context.Request.IsLocal;
        public string UserAgent { get; }
        public bool IsWebSocketRequest { get; set; }
        public IPEndPoint LocalEndPoint { get; }
    }
}
#endif