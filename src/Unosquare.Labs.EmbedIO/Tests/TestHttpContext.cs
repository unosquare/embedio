namespace Unosquare.Labs.EmbedIO.Tests
{
    /// <summary>
    /// Represents a Test Http Context.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.IHttpContext" />
    public class TestHttpContext : IHttpContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpContext"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="webserver">The webserver.</param>
        public TestHttpContext(IHttpRequest request, IWebServer webserver)
        {
            Request = request;
            WebServer = webserver;
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }
        
        /// <inheritdoc />
        public IHttpResponse Response { get; } = new TestHttpResponse();

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }
    }
}