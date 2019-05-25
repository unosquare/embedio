using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Represents our tiny web server used to handle requests.
    ///
    /// This is the default implementation of <c>IWebServer</c> and it's ready to select
    /// the <c>IHttpListener</c> implementation via the proper constructor.
    ///
    /// By default, the WebServer will use the Regex RoutingStrategy for
    /// all registered modules (<c>IWebModule</c>) and EmbedIO Listener (<c>HttpListenerMode</c>).
    /// </summary>
    public class WebServer : WebServerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        ///
        /// Default settings are Regex RoutingStrategy, EmbedIO HttpListenerMode, and binding all
        /// network interfaces with HTTP protocol and default port (http://*:80/).
        /// </summary>
        public WebServer()
            : this(80)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// 
        /// Default settings are Regex RoutingStrategy, EmbedIO HttpListenerMode, and binding all
        /// network interfaces with HTTP protocol with the selected port (http://*:{port}/).
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port)
            : this(new[] { $"http://*:{port}/" })
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        ///
        /// Default settings are Regex RoutingStrategy and EmbedIO HttpListenerMode.
        /// </summary>
        /// <remarks>
        /// <c>urlPrefix</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServer(string urlPrefix)
            : this(new[] { urlPrefix })
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        ///
        /// Default settings are Regex RoutingStrategy and EmbedIO HttpListenerMode.
        /// </summary>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        public WebServer(string[] urlPrefixes)
         : this(urlPrefixes, HttpListenerFactory.Create(HttpListenerMode.EmbedIO))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="mode">The mode.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, HttpListenerMode mode)
            : this(urlPrefixes, HttpListenerFactory.Create(mode))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="certificate">The certificate.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, HttpListenerMode mode, X509Certificate certificate)
            : this(urlPrefixes, HttpListenerFactory.Create(mode, certificate))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="options">The WebServer options.</param>
        public WebServer(WebServerOptions options)
        : this(options.UrlPrefixes, HttpListenerFactory.Create(options.Mode, options.Certificate))
        {
            // temp placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="httpListener">The HTTP listener.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, IHttpListener httpListener)
        {
            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            $"Running HTTPListener: {httpListener.Name}".Info(nameof(WebServer));

            Listener = httpListener;

            foreach (var prefix in urlPrefixes)
            {
                var urlPrefix = new string(prefix?.ToCharArray());

                if (urlPrefix.EndsWith("/") == false) urlPrefix = urlPrefix + "/";
                urlPrefix = urlPrefix.ToLowerInvariant();

                Listener.AddPrefix(urlPrefix);
                $"Web server prefix '{urlPrefix}' added.".Info(nameof(WebServer));
            }

            "Finished Loading Web Server.".Info(nameof(WebServer));
        }

        /// <summary>
        /// Gets the underlying HTTP listener.
        /// </summary>
        /// <value>
        /// The listener.
        /// </value>
        public IHttpListener Listener { get; protected set; }

        /// <summary>
        /// Gets the URL Prefix for which the server is serving requests.
        /// </summary>
        /// <value>
        /// The URL prefix.
        /// </value>
        public List<string> UrlPrefixes => Listener.Prefixes;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Listener.Dispose();
                }
                finally
                {
                    Listener = null;
                }

                "Listener Closed.".Info(nameof(WebServer));
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void Prepare(CancellationToken cancellationToken)
        {
            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            "Started HTTP Listener".Info(nameof(WebServer));

            // close port when the cancellation token is cancelled
            cancellationToken.Register(() => Listener?.Stop());
        }

        /// <inheritdoc />
        protected override bool ShouldProcessMoreRequests() => Listener?.IsListening ?? false;

        /// <inheritdoc />
        protected override Task<IHttpContextImpl> GetContextAsync(CancellationToken cancellationToken) => Listener.GetContextAsync(cancellationToken);

        /// <inheritdoc />
        protected override void OnException() => Listener?.Dispose();
    }
}