using System;
using EmbedIO.Sessions;

namespace EmbedIO
{
    partial class WebServerExtensions
    {
        /// <summary>
        /// Sets the session manager on an <see cref="IWebServer"/>.
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <see cref="IWebServer"/> on which this method is called.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <returns><paramref name="this"/> with the session manager set.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        public static TWebServer WithSessionManager<TWebServer>(this TWebServer @this, ISessionManager sessionManager)
            where TWebServer : IWebServer
        {
            @this.SessionManager = sessionManager;
            return @this;
        }

        /// <summary>
        /// <para>Creates a <see cref="LocalSessionManager"/> with all properties set to their default values
        /// and sets it as session manager on an <see cref="IWebServer"/>.</para>
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <see cref="IWebServer"/> on which this method is called.</param>
        /// <param name="configure">A callback used to configure the session manager.</param>
        /// <returns><paramref name="this"/> with the session manager set.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        public static TWebServer WithLocalSessionManager<TWebServer>(this TWebServer @this, Action<LocalSessionManager>? configure = null)
            where TWebServer : IWebServer
        {
            var sessionManager = new LocalSessionManager();
            configure?.Invoke(sessionManager);
            @this.SessionManager = sessionManager;
            return @this;
        }
    }
}