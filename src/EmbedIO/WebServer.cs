using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using Swan.Logging;

namespace EmbedIO
{
    /// <summary>
    /// <para>EmbedIO's web server. This is the default implementation of <see cref="IWebServer"/>.</para>
    /// <para>This class also contains some useful constants related to EmbedIO's internal working.</para>
    /// </summary>
    public partial class WebServer : WebServerBase<WebServerOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class,
        /// that will respond on HTTP port 80 on all network interfaces.
        /// </summary>
        public WebServer()
            : this(80)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class,
        /// that will respond on the specified HTTP port on all network interfaces.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port)
            : this($"http://*:{port}/")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class
        /// with the specified URL prefixes.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes to configure.</param>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefixes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is already registered.</para>
        /// </exception>
        public WebServer(params string[] urlPrefixes)
            : this(new WebServerOptions().WithUrlPrefixes(urlPrefixes))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="mode">The type of HTTP listener to configure.</param>
        /// <param name="urlPrefixes">The URL prefixes to configure.</param>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefixes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is already registered.</para>
        /// </exception>
        public WebServer(HttpListenerMode mode, params string[] urlPrefixes)
            : this(new WebServerOptions().WithMode(mode).WithUrlPrefixes(urlPrefixes))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="mode">The type of HTTP listener to configure.</param>
        /// <param name="certificate">The X.509 certificate to use for SSL connections.</param>
        /// <param name="urlPrefixes">The URL prefixes to configure.</param>
        /// <exception cref="ArgumentNullException"><paramref name="urlPrefixes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para>One or more of the elements of <paramref name="urlPrefixes"/> is already registered.</para>
        /// </exception>
        public WebServer(HttpListenerMode mode, X509Certificate2 certificate, params string[] urlPrefixes)
            : this(new WebServerOptions()
                .WithMode(mode)
                .WithCertificate(certificate)
                .WithUrlPrefixes(urlPrefixes))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="options">A <see cref="WebServerOptions"/> object used to configure this instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public WebServer(WebServerOptions options)
            : base(options)
        {
            Listener = CreateHttpListener();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="configure">A callback that will be used to configure
        /// the server's options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        public WebServer(Action<WebServerOptions> configure)
            : base(configure)
        {
            Listener = CreateHttpListener();
        }

        /// <summary>
        /// Gets the underlying HTTP listener.
        /// </summary>
        public IHttpListener Listener { get; }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Listener.Dispose();
                }
                catch (Exception ex)
                {
                    ex.Log(LogSource, "Exception thrown while disposing HTTP listener.");
                }

                "Listener closed.".Info(LogSource);
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void Prepare(CancellationToken cancellationToken)
        {
            Listener.Start();
            "Started HTTP Listener".Info(LogSource);

            // close port when the cancellation token is cancelled
            cancellationToken.Register(() => Listener?.Stop());
        }

        /// <inheritdoc />
        protected override async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && (Listener?.IsListening ?? false))
            {
                var context = await Listener.GetContextAsync(cancellationToken).ConfigureAwait(false);
                context.CancellationToken = cancellationToken;
                context.Route = RouteMatch.UnsafeFromRoot(UrlPath.Normalize(context.Request.Url.AbsolutePath, false));

#pragma warning disable CS4014 // Call is not awaited - of course, it has to run in parallel.
                Task.Run(() => DoHandleContextAsync(context), cancellationToken);
#pragma warning restore CS4014
            }
        }

        /// <inheritdoc />
        protected override void OnFatalException() => Listener?.Dispose();

        private IHttpListener CreateHttpListener()
        {
            IHttpListener DoCreate() => Options.Mode switch {
                HttpListenerMode.Microsoft => System.Net.HttpListener.IsSupported 
                    ? new SystemHttpListener(new System.Net.HttpListener()) as IHttpListener 
                    : new Net.HttpListener(Options.Certificate),
                _ => new Net.HttpListener(Options.Certificate)
            };

            var listener = DoCreate();
            $"Running HTTPListener: {listener.Name}".Info(LogSource);

            foreach (var prefix in Options.UrlPrefixes)
            {
                var urlPrefix = new string(prefix?.ToCharArray());

                if (!urlPrefix.EndsWith("/")) urlPrefix += "/";
                urlPrefix = urlPrefix.ToLowerInvariant();

                listener.AddPrefix(urlPrefix);
                $"Web server prefix '{urlPrefix}' added.".Info(LogSource);
            }

            return listener;
        }
    }
}