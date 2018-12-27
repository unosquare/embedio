namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Options for WebServer creation.
    /// </summary>
    public class WebServerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerOptions" /> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServerOptions(string urlPrefix)
            : this(new[] {urlPrefix})
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerOptions"/> class.
        /// </summary>
        /// <param name="urlPrefixes">The urls.</param>
        public WebServerOptions(string[] urlPrefixes)
        {
            UrlPrefixes = urlPrefixes;
        }

        /// <summary>
        /// Gets the URL prefixes.
        /// </summary>
        /// <value>
        /// The URL prefixes.
        /// </value>
        public string[] UrlPrefixes { get; }

        /// <summary>
        /// Gets or sets the routing strategy.
        /// </summary>
        /// <value>
        /// The routing strategy.
        /// </value>
        public RoutingStrategy RoutingStrategy { get; set; } = RoutingStrategy.Regex;

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public HttpListenerMode Mode { get; set; } = HttpListenerMode.EmbedIO;

        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        public X509Certificate Certificate { get; set; }
    }
}