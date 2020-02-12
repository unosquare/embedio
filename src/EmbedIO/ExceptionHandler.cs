using System;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Web;
using Swan.Logging;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard handlers for unhandled exceptions at both module and server level.
    /// </summary>
    /// <seealso cref="IWebServer.OnUnhandledException"/>
    /// <seealso cref="IWebModule.OnUnhandledException"/>
    public static class ExceptionHandler
    {
        /// <summary>
        /// The name of the response header used by the <see cref="EmptyResponseWithHeaders" />
        /// handler to transmit the type of the exception to the client.
        /// </summary>
        public const string ExceptionTypeHeaderName = "X-Exception-Type";

        /// <summary>
        /// The name of the response header used by the <see cref="EmptyResponseWithHeaders" />
        /// handler to transmit the message of the exception to the client.
        /// </summary>
        public const string ExceptionMessageHeaderName = "X-Exception-Message";

        /// <summary>
        /// Gets or sets the contact information to include in exception responses.
        /// </summary>
        public static string? ContactInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include stack traces
        /// in exception responses.
        /// </summary>
        public static bool IncludeStackTraces { get; set; }

        /// <summary>
        /// <para>Gets the default handler used by <see cref="WebServerBase{TOptions}"/>.</para>
        /// <para>This is the same as <see cref="HtmlResponse"/>.</para>
        /// </summary>
        public static ExceptionHandlerCallback Default { get; } = HtmlResponse;

        /// <summary>
        /// Sends an empty <c>500 Internal Server Error</c> response.
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
#pragma warning disable CA1801 // Unused parameter
        public static Task EmptyResponse(IHttpContext context, Exception exception)
#pragma warning restore CA1801
        {
            context.Response.SetEmptyResponse((int)HttpStatusCode.InternalServerError);
            return Task.CompletedTask;
        }

        /// <summary>
        /// <para>Sends an empty <c>500 Internal Server Error</c> response,
        /// with the following additional headers:</para>
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
        /// <para>The aforementioned header names are available as the <see cref="ExceptionTypeHeaderName" /> and
        /// <see cref="ExceptionMessageHeaderName" /> properties, respectively.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task EmptyResponseWithHeaders(IHttpContext context, Exception exception)
        {
            context.Response.SetEmptyResponse((int)HttpStatusCode.InternalServerError);
            context.Response.Headers[ExceptionTypeHeaderName] = Uri.EscapeDataString(exception.GetType().Name);
            context.Response.Headers[ExceptionMessageHeaderName] = Uri.EscapeDataString(exception.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a <c>500 Internal Server Error</c> response with a HTML payload
        /// briefly describing the error, including contact information and/or a stack trace
        /// if specified via the <see cref="ContactInformation"/> and <see cref="IncludeStackTraces"/>
        /// properties, respectively.
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="exception">The unhandled exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task HtmlResponse(IHttpContext context, Exception exception)
            => context.SendStandardHtmlAsync(
                (int)HttpStatusCode.InternalServerError,
                text =>
                {
                    text.Write("<p>The server has encountered an error and was not able to process your request.</p>");
                    text.Write("<p>Please contact the server administrator");

                    if (!string.IsNullOrEmpty(ContactInformation))
                        text.Write(" ({0})", HttpUtility.HtmlEncode(ContactInformation));

                    text.Write(", informing them of the time this error occurred and the action(s) you performed that resulted in this error.</p>");
                    text.Write("<p>The following information may help them in finding out what happened and restoring full functionality.</p>");
                    text.Write(
                        "<p><strong>Exception type:</strong> {0}<p><strong>Message:</strong> {1}",
                        HttpUtility.HtmlEncode(exception.GetType().FullName ?? "<unknown>"),
                        HttpUtility.HtmlEncode(exception.Message));

                    if (IncludeStackTraces)
                    {
                        text.Write(
                            "</p><p><strong>Stack trace:</strong></p><br><pre>{0}</pre>",
                            HttpUtility.HtmlEncode(exception.StackTrace));
                    }
                });

        internal static async Task Handle(string logSource, IHttpContext context, Exception exception, ExceptionHandlerCallback? handler, HttpExceptionHandlerCallback? httpHandler)
        {
            if (handler == null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                return;
            }

            exception.Log(logSource, $"[{context.Id}] Unhandled exception.");

            try
            {
                context.Response.SetEmptyResponse((int)HttpStatusCode.InternalServerError);
                context.Response.DisableCaching();
                await handler(context, exception)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpListenerException)
            {
                throw;
            }
            catch (Exception httpException) when (httpException is IHttpException httpException1)
            {
                if (httpHandler == null)
                    throw;

                await httpHandler(context, httpException1).ConfigureAwait(false);
            }
            catch (Exception exception2)
            {
                exception2.Log(logSource, $"[{context.Id}] Unhandled exception while handling exception.");
            }
        }
    }
}
