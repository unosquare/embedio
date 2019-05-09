using System;
using System.Security.Cryptography.X509Certificates;

namespace EmbedIO.Internal
{
    /// <summary>
    /// Represents a Factory to create a HTTP Listener.
    /// </summary>
    internal static class HttpListenerFactory
    {
        /// <summary>
        /// Creates this instance with the default mode.
        /// The default HTTP Listener is Microsoft for netstandard2.0 target frameworks, otherwise EmbedIO.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>
        /// A HTTP Listener.
        /// </returns>
        public static IHttpListener Create(X509Certificate certificate = null) => Create(HttpListenerMode.Microsoft, certificate);

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
                case HttpListenerMode.Microsoft:
                    if (System.Net.HttpListener.IsSupported)
                        return new HttpListener(new System.Net.HttpListener());

                    return new Net.HttpListener(certificate);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid HTTP Listener mode.");
            }
        }
    }
}