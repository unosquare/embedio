﻿using System;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Swan.Logging;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard handlers for HTTP exceptions at both module and server level.
    /// </summary>
    /// <remarks>
    /// <para>Where applicable, HTTP exception handlers defined in this class
    /// use the <see cref="ExceptionHandler.ContactInformation"/> and
    /// <see cref="ExceptionHandler.IncludeStackTraces"/> properties to customize
    /// their behavior.</para>
    /// </remarks>
    /// <seealso cref="IWebServer.OnHttpException"/>
    /// <seealso cref="IWebModule.OnHttpException"/>
    public static class HttpExceptionHandler
    {
        /// <summary>
        /// <para>Gets the default handler used by <see cref="WebServerBase{TOptions}"/>.</para>
        /// <para>This is the same as <see cref="HtmlResponse"/>.</para>
        /// </summary>
        public static HttpExceptionHandlerCallback Default { get; } = HtmlResponse;

        /// <summary>
        /// Sends an empty response.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="httpException">The HTTP exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task EmptyResponse(IHttpContext context, IHttpException httpException)
            => Task.CompletedTask;

        /// <summary>
        /// Sends a HTTP exception's <see cref="IHttpException.Message">Message</see> property
        /// as a plain text response.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="httpException">The HTTP exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task PlainTextResponse(IHttpContext context, IHttpException httpException)
            => context.SendStringAsync(httpException.Message, MimeType.PlainText, Encoding.UTF8);

        /// <summary>
        /// Sends a response with a HTML payload
        /// briefly describing the error, including contact information and/or a stack trace
        /// if specified via the <see cref="ExceptionHandler.ContactInformation"/>
        /// and <see cref="ExceptionHandler.IncludeStackTraces"/> properties, respectively.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="httpException">The HTTP exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task HtmlResponse(IHttpContext context, IHttpException httpException)
            => context.SendStandardHtmlAsync(
                httpException.StatusCode,
                text => {
                    text.Write(
                        "<p><strong>Exception type:</strong> {0}<p><strong>Message:</strong> {1}",
                        HttpUtility.HtmlEncode(httpException.GetType().FullName ?? "<unknown>"),
                        HttpUtility.HtmlEncode(httpException.Message));

                    text.Write("<hr><p>If this error is completely unexpected to you, and you think you should not seeing this page, please contact the server administrator");

                    if (!string.IsNullOrEmpty(ExceptionHandler.ContactInformation))
                        text.Write(" ({0})", HttpUtility.HtmlEncode(ExceptionHandler.ContactInformation));

                    text.Write(", informing them of the time this error occurred and the action(s) you performed that resulted in this error.</p>");

                    if (ExceptionHandler.IncludeStackTraces)
                    {
                        text.Write(
                            "</p><p><strong>Stack trace:</strong></p><br><pre>{0}</pre>",
                            HttpUtility.HtmlEncode(httpException.StackTrace));
                    }
                });
        
        /// <summary>
        /// Sends a HTTP exception's <see cref="IHttpException.DataObject">DataObject</see> property
        /// as a json/application response.
        ///
        /// If the DataObject is null, the <see cref="IHttpException.Message">Message</see> property will be send.
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
        /// <param name="httpException">The HTTP exception.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        public static Task JsonDataResponse(IHttpContext context, IHttpException httpException)
            => context.SendDataAsync(httpException.DataObject ?? httpException.Message);

        internal static async Task Handle(string logSource, IHttpContext context, Exception exception, HttpExceptionHandlerCallback handler)
        {
            if (handler == null || !(exception is IHttpException httpException))
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                return;
            }

            exception.Log(logSource, $"[{context.Id}] HTTP exception {httpException.StatusCode}");

            try
            {
                context.Response.SetEmptyResponse(httpException.StatusCode);
                context.Response.DisableCaching();
                httpException.PrepareResponse(context);
                await handler(context, httpException)
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
            catch (Exception exception2)
            {
                exception2.Log(logSource, $"[{context.Id}] Unhandled exception while handling HTTP exception {httpException.StatusCode}");
            }
        }
    }
}