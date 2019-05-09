using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Net;
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
        /// <param name="strategy">The strategy.</param>
        public WebServer(int port, RoutingStrategy strategy = RoutingStrategy.Regex)
            : this(new[] { $"http://*:{port}/" }, strategy)
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
        /// <param name="strategy">The strategy.</param>
        public WebServer(string urlPrefix, RoutingStrategy strategy = RoutingStrategy.Regex)
            : this(new[] { urlPrefix }, strategy)
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
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy = RoutingStrategy.Regex)
         : this(urlPrefixes, routingStrategy, HttpListenerFactory.Create(HttpListenerMode.EmbedIO))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <param name="mode">The mode.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy, HttpListenerMode mode)
            : this(urlPrefixes, routingStrategy, HttpListenerFactory.Create(mode))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="certificate">The certificate.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy, HttpListenerMode mode, X509Certificate certificate)
            : this(urlPrefixes, routingStrategy, HttpListenerFactory.Create(mode, certificate))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="options">The WebServer options.</param>
        public WebServer(WebServerOptions options)
        : this(options.UrlPrefixes, options.RoutingStrategy, HttpListenerFactory.Create(options.Mode, options.Certificate))
        {
            // temp placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <param name="httpListener">The HTTP listener.</param>
        /// <exception cref="ArgumentException">Validate urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy, IHttpListener httpListener)
            : base(routingStrategy)
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
        protected override void Prepare(CancellationToken ct)
        {
            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            "Started HTTP Listener".Info(nameof(WebServer));

            // close port when the cancellation token is cancelled
            ct.Register(() => Listener?.Stop());
        }

        /// <inheritdoc />
        protected override async Task RunInternalAsync(CancellationToken ct)
        {
            // Disposing the web server will close the listener.           
            while (Listener != null && Listener.IsListening && !ct.IsCancellationRequested)
            {
                try
                {
                    var context = await Listener.GetContextAsync(ct).ConfigureAwait(false);

                    if (ct.IsCancellationRequested)
                        return;

                    context.WebServer = this;
#pragma warning disable CS4014
                    HandleClientRequest(context, ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                catch (Exception ex)
                {
                    Listener?.Dispose();

                    if (ex is OperationCanceledException || ex is ObjectDisposedException ||
                        ex is HttpListenerException)
                    {
                        if (!ct.IsCancellationRequested)
                            throw;

                        return;
                    }

                    ex.Log(nameof(WebServer));
                }
            }
        }
    }
}