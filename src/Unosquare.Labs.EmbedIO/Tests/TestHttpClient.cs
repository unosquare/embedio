namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a HTTP Client for unit testing.
    /// </summary>
    /// <seealso cref="T:Unosquare.Labs.EmbedIO.IHttpContext" />
    public class TestHttpClient
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

        /// <summary>
        /// Gets or sets the web server.
        /// </summary>
        /// <value>
        /// The web server.
        /// </value>
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
        public async Task<string> GetAsync(string url)
        {
            var response = await SendAsync(new TestHttpRequest($"http://test/{url}"));

            return Encoding.GetString((response.OutputStream as MemoryStream)?.ToArray());
        }

        /// <summary>
        /// Sends the HTTP request asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A task representing the HTTP response.</returns>
        /// <exception cref="InvalidOperationException">The IWebServer implementation should be TestWebServer.</exception>
        public async Task<TestHttpResponse> SendAsync(TestHttpRequest request)
        {   
            var context = new TestHttpContext(request, WebServer);

            if (!(WebServer is TestWebServer testServer))
                throw new InvalidOperationException("The IWebServer implementation should be TestWebServer.");

            testServer.HttpContexts.Enqueue(context);

            try
            {
                while (context.Response.OutputStream.Position == 0)
                    await Task.Delay(1);
            }
            catch
            {
                // ignore
            }

            return context.Response as TestHttpResponse;
        }
    }
}