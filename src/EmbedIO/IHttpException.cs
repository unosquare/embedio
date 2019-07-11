using System;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents an exception that results in a particular
    /// HTTP response to be sent to the client.</para>
    /// <para>This interface is meant to be implemented
    /// by classes derived from <see cref="Exception" />.</para>
    /// </summary>
    public interface IHttpException
    {
        /// <summary>
        /// Gets the response status code for this HTTP exception.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Gets a message that can be included in the response.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the stack trace of the HTTP exception.
        /// </summary>
        string StackTrace { get; }

        /// <summary>
        /// Sets necessary headers, as required by the nature
        /// of the HTTP exception (e.g. <c>Location</c> for
        /// <see cref="HttpRedirectException" />).
        /// </summary>
        /// <param name="context">The HTTP context of the response.</param>
        void PrepareResponse(IHttpContext context);
    }
}