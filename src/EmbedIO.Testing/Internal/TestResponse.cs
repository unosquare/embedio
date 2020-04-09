using System;
using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO.Testing.Internal
{
    internal sealed class TestResponse : IHttpResponse, IDisposable
    {
        ~TestResponse()
        {
            Dispose(false);
        }

        public WebHeaderCollection Headers { get; } = new WebHeaderCollection();

        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

        public long ContentLength64 { get; set; }

        public string? ContentType { get; set; }

        public Stream OutputStream { get; } = new MemoryStream();

        public ICookieCollection Cookies { get; } = new Net.CookieList();

        public Encoding? ContentEncoding { get; set; } = WebServer.DefaultEncoding;

        public bool KeepAlive { get; set; }

        public bool SendChunked { get; set; }

        public Version ProtocolVersion { get; } = HttpVersion.Version11;

        public byte[]? Body { get; private set; }

        public string? StatusDescription { get; set; }
        
        internal bool IsClosed { get; private set; }

        public void SetCookie(Cookie cookie) => Cookies.Add(cookie);

        public void Close()
        {
            IsClosed = true;
            Body = (OutputStream as MemoryStream)?.ToArray();

            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            OutputStream?.Dispose();
        }
    }
}