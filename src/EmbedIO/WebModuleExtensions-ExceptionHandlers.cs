using System;

namespace EmbedIO
{
    partial class WebModuleExtensions
    {
        /// <summary>
        /// Sets the HTTP exception handler on an <see cref="IWebModule" />.
        /// </summary>
        /// <typeparam name="TWebModule">The type of the web server.</typeparam>
        /// <param name="this">The <typeparamref name="TWebModule" /> on which this method is called.</param>
        /// <param name="handler">The HTTP exception handler.</param>
        /// <returns><paramref name="this"/> with the <see cref="IWebModule.OnHttpException">OnHttpException</see>
        /// property set to <paramref name="handler" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <seealso cref="IWebModule.OnHttpException" />
        /// <seealso cref="HttpExceptionHandler" />
        public static TWebModule HandleHttpException<TWebModule>(this TWebModule @this, HttpExceptionHandlerCallback handler)
            where TWebModule : IWebModule
        {
            @this.OnHttpException = handler;
            return @this;
        }

        /// <summary>
        /// Sets the unhandled exception handler on an <see cref="IWebModule" />.
        /// </summary>
        /// <typeparam name="TWebModule">The type of the web server.</typeparam>
        /// <param name="this">The <typeparamref name="TWebModule" /> on which this method is called.</param>
        /// <param name="handler">The unhandled exception handler.</param>
        /// <returns><paramref name="this"/> with the <see cref="IWebModule.OnUnhandledException">OnUnhandledException</see>
        /// property set to <paramref name="handler" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <seealso cref="IWebModule.OnUnhandledException" />
        /// <seealso cref="ExceptionHandler" />
        public static TWebModule HandleUnhandledException<TWebModule>(this TWebModule @this, ExceptionHandlerCallback handler)
            where TWebModule : IWebModule
        {
            @this.OnUnhandledException = handler;
            return @this;
        }
    }
}