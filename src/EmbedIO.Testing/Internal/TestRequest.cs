using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using EmbedIO.Net;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO.Testing.Internal
{
    internal class TestRequest : IHttpRequest
    {
        private readonly HttpContent _content;

        public TestRequest(HttpRequestMessage clientRequest)
        {
            _content = Validate.NotNull(nameof(clientRequest), clientRequest).Content;

            var headers = new NameValueCollection();
            foreach (var pair in clientRequest.Headers)
            {
                var values = pair.Value.ToArray();
                switch (values.Length)
                {
                    case 0:
                        headers.Add(pair.Key, string.Empty);
                        break;
                    case 1:
                        headers.Add(pair.Key, values[0]);
                        break;
                    default:
                        foreach (var value in values)
                            headers.Add(pair.Key, value);

                        break;
                }

                if (pair.Key == HttpHeaderNames.Cookie) Cookies = CookieList.Parse(string.Join(",", values));
            }

            Headers = headers;
            if (Cookies == null)
                Cookies = new CookieList();

            ProtocolVersion = clientRequest.Version;
            KeepAlive = !(clientRequest.Headers.ConnectionClose ?? true);
            RawUrl = clientRequest.RequestUri.PathAndQuery;
            QueryString = UrlEncodedDataParser.Parse(clientRequest.RequestUri.Query, true);
            HttpMethod = clientRequest.Method.ToString();
            HttpVerb = HttpMethodToVerb(clientRequest.Method);
            Url = clientRequest.RequestUri;
            HasEntityBody = _content != null;
            ContentEncoding = Encoding.GetEncoding(_content?.Headers.ContentType?.CharSet ?? WebServer.DefaultEncoding.WebName);
            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 9999);
            UserAgent = clientRequest.Headers.UserAgent?.ToString();
            LocalEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);
            ContentType = _content?.Headers.ContentType?.MediaType;
        }

        public ICookieCollection Cookies { get; }

        public Version ProtocolVersion { get; }

        public NameValueCollection Headers { get; }

        public bool KeepAlive { get; }

        public string RawUrl { get; }

        public NameValueCollection QueryString { get; }

        public string HttpMethod { get; }

        public HttpVerbs HttpVerb { get; }

        public Uri Url { get; }

        public bool HasEntityBody { get; }

        public Stream? InputStream => _content?.ReadAsStreamAsync().Await();

        public Encoding ContentEncoding { get; }

        public IPEndPoint RemoteEndPoint { get; }

        public bool IsLocal => true;

        public bool IsSecureConnection => false;

        public string? UserAgent { get; }

        public bool IsWebSocketRequest => false;

        public IPEndPoint LocalEndPoint { get; }

        public string? ContentType { get; }

        public long ContentLength64 => 0;

        public bool IsAuthenticated => false;

        public Uri? UrlReferrer => null;

        private static HttpVerbs HttpMethodToVerb(HttpMethod method)
        {
            if (method == System.Net.Http.HttpMethod.Delete)
                return HttpVerbs.Delete;

            if (method == System.Net.Http.HttpMethod.Get)
                return HttpVerbs.Get;

            if (method == System.Net.Http.HttpMethod.Head)
                return HttpVerbs.Head;

            if (method == System.Net.Http.HttpMethod.Options)
                return HttpVerbs.Options;
            
            if (method == AdditionalHttpMethods.Patch)
                return HttpVerbs.Patch;

            if (method == System.Net.Http.HttpMethod.Post)
                return HttpVerbs.Post;

            if (method == System.Net.Http.HttpMethod.Put)
                return HttpVerbs.Put;

            return HttpVerbs.Any;
        }
    }
}
