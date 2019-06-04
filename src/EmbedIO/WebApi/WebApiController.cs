using System;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Sessions;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active.
    /// </summary>
    public abstract class WebApiController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiController" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected WebApiController(IHttpContext context, CancellationToken cancellationToken)
        {
            HttpContext = context;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets the HTTP context.
        /// </summary>
        protected IHttpContext HttpContext { get; }

        /// <summary>
        /// Gets the cancellation token used to cancel processing of the request.
        /// </summary>
        protected CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        protected IHttpRequest Request => HttpContext.Request;

        /// <summary>
        /// Gets the HTTP response object.
        /// </summary>
        protected IHttpResponse Response => HttpContext.Response;

        /// <summary>
        /// Gets the user.
        /// </summary>
        protected IPrincipal User => HttpContext.User;

        /// <summary>
        /// Gets the session proxy associated with the HTTP context.
        /// </summary>
        protected ISessionProxy Session => HttpContext.Session;

        /// <summary>
        /// <para>This method is meant to be called internally by EmbedIO.</para>
        /// <para>Derived classes can override the <see cref="OnBeforeHandler"/> method
        /// to perform common operations before any handler gets called.</para>
        /// </summary>
        /// <seealso cref="OnBeforeHandler"/>
        public void PreProcessRequest() => OnBeforeHandler();

        /// <summary>
        /// <para>Called before a handler to perform common operations.</para>
        /// <para>The default behavior is to set response headers
        /// in order to prevent caching of the response.</para>
        /// </summary>
        protected virtual void OnBeforeHandler() => HttpContext.Response.DisableCaching();

        /// <summary>
        /// Outputs async a Json Response given a data object.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// A <c>true</c> value if the response output was set.
        /// </returns>
        protected virtual Task<bool> Ok(object data) => HttpContext.SendDataAsync(data, CancellationToken);

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="transformFunc">The transform function.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        protected virtual Task<bool> Ok<TIn, TOut>(Func<TIn, CancellationToken, Task<TOut>> transformFunc)
            where TIn : class
            => HttpContext.TransformJsonAsync(transformFunc, CancellationToken);

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        protected virtual Task<bool> Ok(
            string content,
            string contentType = MimeTypes.JsonType,
            Encoding encoding = null,
            CancellationToken cancellationToken = default)
            => HttpContext.SendStringAsync(content, contentType, encoding, cancellationToken);
    }
}