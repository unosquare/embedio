﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard handlers for unhandled exceptions at both module and server level.
    /// </summary>
    /// <seealso cref="IWebServer.OnUnhandledException"/>
    /// <seealso cref="IWebModule.OnUnhandledException"/>
    public static class StandardExceptionHandlers
    {
        /// <summary>
        /// Gets or sets the contact information to include in exception responses.
        /// </summary>
        public static string ContactInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include stack traces
        /// in exception responses.
        /// </summary>
        public static bool IncludeStackTraces { get; set; }

        /// <summary>
        /// <para>Gets the default handler used by <see cref="WebServerBase"/>.</para>
        /// <para>This is the same as <see cref="HtmlResponse"/>.</para>
        /// </summary>
        public static WebExceptionHandler Default { get; } = HtmlResponse;

        /// <summary>
        /// Sends an empty <c>500 Internal Server Error</c> response.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="path">The URL path requested by the client.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task EmptyResponse(IHttpContext context, string path, Exception exception, CancellationToken cancellationToken)
        {
            context.Response.SetEmptyResponse((int) HttpStatusCode.InternalServerError);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends an empty <c>500 Internal Server Error</c> response,
        /// with the following additional headers:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Header</term>
        ///     <description>Value</description>
        ///   </listheader>
        ///   <item>
        ///     <term><c>X-Exception-Type</c></term>
        ///     <description>The name (without namespace) of the type of exception that was thrown.</description>
        ///   </item>
        ///   <item>
        ///     <term><c>X-Exception-Message</c></term>
        ///     <description>The <see cref="Exception.Message">Message</see> property of the exception.</description>
        ///   </item>
        /// </list>
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="path">The URL path requested by the client.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task EmptyResponseWithHeaders(IHttpContext context, string path, Exception exception, CancellationToken cancellationToken)
        {
            context.Response.SetEmptyResponse((int)HttpStatusCode.InternalServerError);
            context.Response.Headers["X-Exception-Type"] = Uri.EscapeDataString(exception.GetType().Name);
            context.Response.Headers["X-Exception-Message"] = Uri.EscapeDataString(exception.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a <c>500 Internal Server Error</c> response with a HTML payload
        /// briefly describing the error, including contact information and/or a stack trace
        /// if specified via the <see cref="ContactInformation"/> and <see cref="IncludeStackTraces"/>
        /// properties, respectively.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="path">The URL path requested by the client.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task HtmlResponse(IHttpContext context, string path, Exception exception, CancellationToken cancellationToken)
            => context.Response.SendStandardHtmlAsync(
                (int)HttpStatusCode.InternalServerError,
                sb => {
                    sb.Append("<p>The server has encountered an error and was not able to process your request.</p>")
                        .Append("<p>Please contact the server administrator");

                    if (!string.IsNullOrEmpty(ContactInformation))
                    {
                        sb.Append(" (")
                            .Append(HttpUtility.HtmlEncode(ContactInformation))
                            .Append(')');
                    }

                    sb.Append(", informing them of the time this error occurred and the action(s) you performed that resulted in this error.</p>")
                        .Append("<p>The following information may help them in finding out what happened and restoring full functionality.</p>")
                        .Append("<p><strong>Exception type:</strong> ")
                        .Append(HttpUtility.HtmlEncode(exception.GetType().FullName ?? "<unknown>"))
                        .Append("<p><strong>Message:</strong> ")
                        .Append(HttpUtility.HtmlEncode(exception.ExceptionMessage()));

                    if (IncludeStackTraces)
                    {
                        sb.Append("</p><p><strong>Stack trace:</strong></p><br><pre>")
                            .Append(HttpUtility.HtmlEncode(exception.StackTrace))
                            .Append("</pre>");
                    }
                },
                cancellationToken);
    }
}