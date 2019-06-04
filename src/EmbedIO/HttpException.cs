using System;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// When thrown, breaks the request handling control flow
    /// and sends an error response to the client.
    /// </summary>
    public partial class HttpException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        public HttpException(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        public HttpException(HttpStatusCode statusCode)
            : this((int)statusCode)
        {
        }

        /// <summary>
        /// The status code to set on the response when this exception is thrown.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Asynchronously sends an error response related to the cause of this exception.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        public Task SendResponseAsync(IHttpContext context)
        {
            context.Response.SetEmptyResponse(StatusCode);
            return OnSendResponseAsync(context);
        }

        /// <summary>
        /// Called by <see cref="SendResponseAsync"/> to add any necessary data
        /// to the response, if required by a derived class.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnSendResponseAsync(IHttpContext context) => Task.CompletedTask;
    }
}