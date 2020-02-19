using System;

namespace EmbedIO.Testing
{
    /// <summary>
    /// Represents an object that can act as a web server, processing requests
    /// directed to a fictional base URL.
    /// </summary>
    /// <seealso cref="IHttpContextHandler" />
    public interface ITestWebServer : IHttpContextHandler
    {
        /// <summary>
        /// Gets the base URL simulated by the server.
        /// </summary>
        Uri BaseUrl { get; }
    }
}