using System;

namespace EmbedIO
{
    partial class WebServerExtensions
    {
        /// <summary>
        /// Sets the HTTP exception handler on an <see cref="IWebServer" />.
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <typeparamref name="TWebServer" /> on which this method is called.</param>
        /// <param name="handler">The HTTP exception handler.</param>
        /// <returns><paramref name="this"/> with the <see cref="IWebServer.OnHttpException">OnHttpException</see>
        /// property set to <paramref name="handler" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler" /> is <see langwrd="null" />.</exception>
        /// <seealso cref="IWebServer.OnHttpException" />
        /// <seealso cref="HttpExceptionHandler" />
        public static TWebServer HandleHttpException<TWebServer>(this TWebServer @this, HttpExceptionHandlerCallback handler)
            where TWebServer : IWebServer
        {
            @this.OnHttpException = handler;
            return @this;
        }

        /// <summary>
        /// Sets the unhandled exception handler on an <see cref="IWebServer" />.
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <typeparamref name="TWebServer" /> on which this method is called.</param>
        /// <param name="handler">The unhandled exception handler.</param>
        /// <returns><paramref name="this"/> with the <see cref="IWebServer.OnUnhandledException">OnUnhandledException</see>
        /// property set to <paramref name="handler" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="handler" /> is <see langwrd="null" />.</exception>
        /// <seealso cref="IWebServer.OnUnhandledException" />
        /// <seealso cref="ExceptionHandler" />
        public static TWebServer HandleUnhandledException<TWebServer>(this TWebServer @this, ExceptionHandlerCallback handler)
            where TWebServer : IWebServer
        {
            @this.OnUnhandledException = handler;
            return @this;
        }
    }
}