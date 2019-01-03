namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Represents a Factory to create a HTTP Listener.
    /// </summary>
    public static class HttpListenerFactory
    {
        /// <summary>
        /// Creates this instance with the default mode.
        /// The default HTTP Listener is Microsoft for net47 and
        /// netstandard2.0 target frameworks, otherwise EmbedIO.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>
        /// A HTTP Listener.
        /// </returns>
        public static IHttpListener Create(X509Certificate certificate = null)
        {
            var mode = HttpListenerMode.EmbedIO;

#if NET47
            mode = HttpListenerMode.Microsoft;
#endif

            return Create(mode, certificate);
        }

        /// <summary>
        /// Creates the specified mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="certificate">The certificate.</param>
        /// <returns>
        /// A HTTP Listener.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">mode - null.</exception>
        public static IHttpListener Create(HttpListenerMode mode, X509Certificate certificate = null)
        {
            switch (mode)
            {
                case HttpListenerMode.EmbedIO:
                    return new Net.HttpListener(certificate);
#if !NETSTANDARD1_3 && !UWP
                case HttpListenerMode.Microsoft:
                    if (System.Net.HttpListener.IsSupported)
                        return new HttpListener(new System.Net.HttpListener());

                    return new Net.HttpListener(certificate);
#endif
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid HTTP Listener mode.");
            }
        }
    }
}
