namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Security.Principal;

    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active.
    /// </summary>
    public abstract class WebApiController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiController"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        protected WebApiController(IHttpContext context)
        {
            HttpContext = context;
        }

        /// <summary>
        /// Gets the HTTP context.
        /// </summary>
        /// <value>
        /// The HTTP context.
        /// </value>
        public IHttpContext HttpContext { get; }

        /// <summary>
        /// Gets the HTTP Request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public IHttpRequest Request => HttpContext.Request;

        /// <summary>
        /// Gets the HTTP Response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public IHttpResponse Response => HttpContext.Response;

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public IPrincipal User => HttpContext.User;

        /// <summary>
        /// Gets or sets the web server.
        /// </summary>
        /// <value>
        /// The web server.
        /// </value>
        public IWebServer WebServer => HttpContext.WebServer;

        /// <summary>
        /// Sets the default headers to the Web API response.
        /// By default will set:
        ///
        /// Expires - Mon, 26 Jul 1997 05:00:00 GMT
        /// LastModified - (Current Date)
        /// CacheControl - no-store, no-cache, must-revalidate
        /// Pragma - no-cache
        ///
        /// Previous values are defined to avoid caching from client.
        /// </summary>
        public virtual void SetDefaultHeaders() => HttpContext.NoCache();

        /// <summary>
        /// Outputs async a Json Response given a data object.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <c>true</c> value if the response output was set.
        /// </returns>
        public virtual Task<bool> JsonResponseAsync(object data, CancellationToken cancellationToken = default) =>
            HttpContext.JsonResponseAsync(data, cancellationToken);

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public virtual Task<bool> TransformJson<TIn, TOut>(Func<TIn, CancellationToken, Task<TOut>> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
            => HttpContext.TransformJson(transformFunc, cancellationToken);

        /// <summary>
        /// Outputs a JSON Response given an exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public virtual Task<bool> JsonExceptionResponseAsync(
            Exception ex,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
            => HttpContext.JsonExceptionResponseAsync(ex, statusCode, useGzip, cancellationToken);

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public virtual Task<bool> StringResponseAsync(
            string content,
            string contentType = "application/json",
            Encoding encoding = null,
            bool useGzip = true,
            CancellationToken cancellationToken = default) =>
            Response.StringResponseAsync(content, contentType, encoding, useGzip, cancellationToken);

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <returns>A task with a collection that represents KVPs from request data.</returns>
        public virtual Task<Dictionary<string, object>> RequestFormDataDictionaryAsync() =>
            HttpContext.RequestFormDataDictionaryAsync();

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        public virtual void DeleteSession() => HttpContext.DeleteSession();
        
        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <returns>A session object for the given server context.</returns>
        public virtual SessionInfo GetSession() => HttpContext.GetSession();
    }
}