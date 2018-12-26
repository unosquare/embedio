namespace Unosquare.Labs.EmbedIO
{
    using System.Security.Cryptography.X509Certificates;
    using Constants;
    using System.Collections.Generic;
    using Swan;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents our tiny web server used to handle requests.
    ///
    /// This is the default implementation of <c>IWebServer</c> and it's ready to select
    /// the <c>IHttpListener</c> implementation via the proper constructor.
    ///
    /// By default, the WebServer will use the Regex RoutingStrategy for
    /// all registered modules (<c>IWebModule</c>) and EmbedIO Listener (<c>HttpListenerMode</c>).
    /// </summary>
    public class WebServer : IWebServer, IDisposable
    {
        private readonly WebModules _modules = new WebModules();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        ///
        /// Default settings are Regex RoutingStrategy, EmbedIO HttpListenerMode, and binding all
        /// network interfaces with HTTP protocol and default port (http://*:80/).
        /// </summary>
        public WebServer()
            : this(new[] { "http://*/" })
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
        /// <exception cref="ArgumentException">Argument urlPrefix must be specified.</exception>
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
        /// <param name="certificate">The certificate.</param>
        /// <exception cref="ArgumentException">Argument urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy, HttpListenerMode mode, X509Certificate certificate = null)
            : this(urlPrefixes, routingStrategy, HttpListenerFactory.Create(mode, certificate))
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <param name="httpListener">The HTTP listener.</param>
        /// <exception cref="ArgumentException">Argument urlPrefix must be specified.</exception>
        /// <remarks>
        /// <c>urlPrefixes</c> must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </remarks>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy, IHttpListener httpListener)
        {
            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            $"Running HTTPListener: {httpListener.GetType()}".Info(nameof(WebServer));

            RoutingStrategy = routingStrategy;
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

        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnMethodNotAllowed { get; set; } = ctx =>
             ctx.HtmlResponseAsync(Responses.Response405Html, System.Net.HttpStatusCode.MethodNotAllowed);

        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnNotFound { get; set; } = ctx =>
            ctx.HtmlResponseAsync(Responses.Response404Html, System.Net.HttpStatusCode.NotFound);

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
        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();

        /// <inheritdoc />
        public ISessionWebModule SessionModule => _modules.SessionModule;

        /// <inheritdoc />
        public RoutingStrategy RoutingStrategy { get; protected set; }

        /// <summary>
        /// Static method to create webserver instance using a single URL prefix.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix) => new WebServer(urlPrefix);

        /// <inheritdoc />
        public T Module<T>()
            where T : class, IWebModule
        {
            return _modules.Module<T>();
        }

        /// <inheritdoc />
        public void RegisterModule(IWebModule module) => _modules.RegisterModule(module, this);

        /// <inheritdoc/>
        public void UnregisterModule(Type moduleType) => _modules.UnregisterModule(moduleType);

        /// <inheritdoc />
        /// <exception cref="T:System.InvalidOperationException">The method was already called.</exception>
        /// <exception cref="T:System.OperationCanceledException">Cancellation was requested.</exception>
        /// <remarks>
        /// Both the server and client requests are queued separately on the thread pool,
        /// so it is safe to call <see cref="M:System.Threading.Tasks.Task.Wait" /> in a synchronous method.
        /// </remarks>
        public async Task RunAsync(CancellationToken ct = default)
        {
            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            "Started HTTP Listener".Info(nameof(WebServer));

            // close port when the cancellation token is cancelled
            ct.Register(() => Listener?.Stop());

            try
            {
                // Init modules
                foreach (var module in _modules)
                {
                    module.Server = this;
                    module.Start(ct);
                }

                // Disposing the web server will close the listener.           
                while (Listener != null && Listener.IsListening && !ct.IsCancellationRequested)
                {
                    try
                    {
                        var clientSocket = await Listener.GetContextAsync(ct).ConfigureAwait(false);

                        if (ct.IsCancellationRequested)
                            return;

                        clientSocket.WebServer = this;

#pragma warning disable CS4014
                        var handler = new HttpHandler(clientSocket);

                        handler.HandleClientRequest(ct);
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
            catch (TaskCanceledException)
            {
                // Ignore
            }
            finally
            {
                "Cleaning up".Info(nameof(WebServer));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Listener == null) return;

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
    }
}