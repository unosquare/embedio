namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Represents a HTTP Client for unit testing.
    /// </summary>
    /// <seealso cref="T:Unosquare.Labs.EmbedIO.IHttpContext" />
    public class TestHttpClient : IHttpContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpClient" /> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="encoding">The encoding.</param>
        public TestHttpClient(IWebServer server, Encoding encoding = null)
        {
            WebServer = server;
            Encoding = encoding ?? Encoding.UTF8;
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; private set; }

        /// <inheritdoc />
        public IHttpResponse Response { get; private set; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets the asynchronous.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>
        /// A task representing the GET call.
        /// </returns>
        /// <exception cref="InvalidOperationException">The IWebServer implementation should be TestWebServer.</exception>
        public async Task<string> GetAsync(string url)
        {
            Request = new TestHttpRequest($"http://test/{url}");
            Response = new TestHttpResponse();

            if (!(WebServer is TestWebServer testServer))
                throw new InvalidOperationException("The IWebServer implementation should be TestWebServer.");

            testServer.HttpContexts.Enqueue(this);

            try
            {
                while (Response.OutputStream.Position == 0)
                    await Task.Delay(1);
            }
            catch
            {
                // ignore
            }

            return Encoding.GetString((Response.OutputStream as MemoryStream)?.ToArray());
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<TestHttpResponse> SendAsync(TestHttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}