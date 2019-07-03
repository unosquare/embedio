using System;
using System.Net;
using System.Text;
using System.Threading;
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
        /// Initializes a new instance of the <see cref="HttpException"/> class,
        /// with no message to include in the response.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        public HttpException(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class,
        /// with no message to include in the response.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        public HttpException(HttpStatusCode statusCode)
            : this((int)statusCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class,
        /// with a message to include in the response.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="message">A message to include in the response as plain text.</param>
        public HttpException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class,
        /// with a message to include in the response.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="message">A message to include in the response as plain text.</param>
        public HttpException(HttpStatusCode statusCode, string message)
            : this((int)statusCode, message)
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        public Task SendResponseAsync(IHttpContext context, CancellationToken cancellationToken)
        {
            context.Response.SetEmptyResponse(StatusCode);
            return OnSendResponseAsync(context, cancellationToken);
        }

        /// <summary>
        /// <para>Called by <see cref="SendResponseAsync"/> to add any necessary data
        /// to the response, if required by a derived class.</para>
        /// <para>The base implementation sends the <see cref="Exception.Message"/> property,
        /// if not null or empty, as UTF-8-encoded plain text.</para>
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnSendResponseAsync(IHttpContext context, CancellationToken cancellationToken)
            => string.IsNullOrEmpty(Message)
                ? Task.CompletedTask
                : context.SendStringAsync(Message, MimeType.PlainText, Encoding.UTF8, cancellationToken);
    }
}