#if NET47
namespace Unosquare.Labs.EmbedIO
{
    using System.IO;
    using System.Net;

    public class HttpResponse : IHttpResponse
    {
        private readonly HttpListenerContext _context;

        public HttpResponse(HttpListenerContext context)
        {
            _context = context;
        }

        public WebHeaderCollection Headers { get; }
        public int StatusCode { get; set; }
        public long ContentLength64 { get; set; }
        public string ContentType { get; set; }
        public Stream OutputStream { get; }
        public CookieCollection Cookies { get; }

        public void AddHeader(string headerName, string value)
        {
            throw new System.NotImplementedException();
        }

        public void SetCookie(Cookie sessionCookie)
        {
            throw new System.NotImplementedException();
        }
    }
}
#endif