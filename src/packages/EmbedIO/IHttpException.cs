using System;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents an exception that results in a particular
    /// HTTP response to be sent to the client.</para>
    /// <para>This interface is meant to be implemented
    /// by classes derived from <see cref="Exception" />.</para>
    /// <para>Either as message or a data object can be attached to
    /// the exception; which one, if any, is sent to the client
    /// will depend upon the handler used to send the response.</para>
    /// </summary>
    /// <seealso cref="HttpExceptionHandlerCallback"/>
    /// <seealso cref="HttpExceptionHandler"/>
    public interface IHttpException
    {
        /// <summary>
        /// Gets the response status code for a HTTP exception.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Gets the stack trace of a HTTP exception.
        /// </summary>
        string StackTrace { get; }

        /// <summary>
        /// <para>Gets a message that can be included in the response triggered
        /// by a HTTP exception.</para>
        /// <para>Whether the message is actually sent to the client will depend
        /// upon the handler used to send the response.</para>
        /// </summary>
        /// <remarks>
        /// <para>Do not rely on <see cref="Exception.Message"/> to implement
        /// this property if you want to support <see langword="null"/> messages,
        /// because a default message will be supplied by the CLR at throw time
        /// when <see cref="Exception.Message"/> is <see langword="null"/>.</para>
        /// </remarks>
        string? Message { get; }

        /// <summary>
        /// <para>Gets an object that can be serialized and included
        /// in the response triggered by a HTTP exception.</para>
        /// <para>Whether the object is actually sent to the client will depend
        /// upon the handler used to send the response.</para>
        /// </summary>
        object? DataObject { get; }

        /// <summary>
        /// Sets necessary headers, as required by the nature
        /// of the HTTP exception (e.g. <c>Location</c> for
        /// <see cref="HttpRedirectException" />).
        /// </summary>
        /// <param name="context">The HTTP context of the response.</param>
        void PrepareResponse(IHttpContext context);
    }
}