using System;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IWebServer"/>.
    /// </summary>
    public static class WebServerExtensions
    {
        /// <summary>
        /// Sets the session manager on a <see cref="IWebServer"/>.
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
        /// <para>Creates a <see cref="LocalSessionManager"/> and sets it as session manager on a <see cref="IWebServer"/>.</para>
        /// <para>The session cookie will have a name of <c>"__session"</c>, a path of <c>"/",</c>
        /// a duration of one hour, and will be hidden from Javascript running on user agents.</para>
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <see cref="IWebServer"/> on which this method is called.</param>
        /// <returns><paramref name="this"/> with the session manager set.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        public static TWebServer WithLocalSessionManager<TWebServer>(this TWebServer @this)
            where TWebServer : IWebServer
        {
            @this.SessionManager = new LocalSessionManager("__session", "/", TimeSpan.FromHours(1), true);
            return @this;
        }

        /// <summary>
        /// Creates a <see cref="LocalSessionManager"/> with the specified parameters
        /// and sets it as session manager on a <see cref="IWebServer"/>.
        /// </summary>
        /// <typeparam name="TWebServer">The type of the web server.</typeparam>
        /// <param name="this">The <see cref="IWebServer"/> on which this method is called.</param>
        /// <param name="cookieName">The name of the session cookie.</param>
        /// <param name="cookiePath">The path of the session cookie.</param>
        /// <param name="cookieDuration">The duration of the session cookie.</param>
        /// <param name="cookieHttpOnly"><see langword="true"/> to hide the session cookie from Javascript running on a user agent.</param>
        /// <returns><paramref name="this"/> with the session manager set.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="cookieName"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="cookiePath"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="cookieName"/> is empty or contains one or more invalid characters.</para>
        /// <para>- or -</para>
        /// <para><paramref name="cookiePath"/> is not a valid base URL path.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">The web server has already been started.</exception>
        public static TWebServer WithLocalSessionManager<TWebServer>(this TWebServer @this, string cookieName, string cookiePath, TimeSpan cookieDuration, bool cookieHttpOnly = true)
            where TWebServer : IWebServer
        {
            @this.SessionManager = new LocalSessionManager(cookieName, cookiePath, cookieDuration, cookieHttpOnly);
            return @this;
        }
    }
}