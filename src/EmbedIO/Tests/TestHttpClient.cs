using System;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Tests.Internal;
using EmbedIO.Utilities;

namespace EmbedIO.Tests
{
    /// <summary>
    /// Represents a HTTP Client for unit testing.
    /// </summary>
    /// <seealso cref="IHttpContext" />
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
        public async Task<string> GetAsync(string url = "")
        {
            var response = await SendAsync(new TestHttpRequest($"http://test/{url}")).ConfigureAwait(false);

            return (response as TestHttpResponse).GetBodyAsString(Encoding);
        }

        /// <summary>
        /// Sends the HTTP request asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A task representing the HTTP response.</returns>
        /// <exception cref="InvalidOperationException">The IWebServer implementation should be TestWebServer.</exception>
        public async Task<IHttpResponse> SendAsync(TestHttpRequest request)
        {
            var testRequest = Validate.NotNull(nameof(request), request) as TestHttpRequest;
            if (testRequest is null)
                throw new ArgumentException($"Request should be a {nameof(TestHttpRequest)}");
            var context = new TestHttpContext(request, WebServer);

            if (!(WebServer is TestWebServer testServer))
                throw new InvalidOperationException($"The {nameof(IWebServer)} implementation should be {nameof(TestWebServer)}.");

            testServer.HttpContexts.Enqueue(context);

            if (!(context.Response is TestHttpResponse response))
                throw new InvalidOperationException("The response object is invalid.");
            
            try
            {
                while (!response.IsClosed)
                    await Task.Delay(1).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }

            return response;
        }
    }
}