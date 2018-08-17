namespace Unosquare.Labs.EmbedIO
{
    /// <summary>
    /// Interface to create a HTTP Context.
    /// </summary>
    public interface IHttpContext
    {
        /// <summary>
        /// Gets the HTTP Request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        IHttpRequest Request { get;  }

        /// <summary>
        /// Gets the HTTP Response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        IHttpResponse Response { get; }
    }
}
