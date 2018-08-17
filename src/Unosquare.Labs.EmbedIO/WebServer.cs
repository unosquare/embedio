namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents our tiny web server used to handle requests.
    /// </summary>
    public class WebServer
        : IDisposable
    {
        private readonly List<IWebModule> _modules = new List<IWebModule>(4);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        public WebServer()
            : this(new[] { "http://*/" })
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="strategy">The strategy.</param>
        public WebServer(int port, RoutingStrategy strategy = RoutingStrategy.Wildcard)
            : this(new[] { $"http://*:{port}/" }, strategy)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="strategy">The strategy.</param>
        public WebServer(string urlPrefix, RoutingStrategy strategy = RoutingStrategy.Wildcard)
            : this(new[] { urlPrefix }, strategy)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <exception cref="InvalidOperationException">The HTTP Listener is not supported in this OS.</exception>
        /// <exception cref="ArgumentException">Argument urlPrefix must be specified.</exception>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy = RoutingStrategy.Wildcard)
        {
            if (HttpListener.IsSupported == false)
                throw new InvalidOperationException("The HTTP Listener is not supported in this OS");

            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            RoutingStrategy = routingStrategy;
            Listener = new HttpListener();

            foreach (var prefix in urlPrefixes)
            {
                var urlPrefix = new String(prefix?.ToCharArray());

                if (urlPrefix.EndsWith("/") == false) urlPrefix = urlPrefix + "/";
                urlPrefix = urlPrefix.ToLowerInvariant();

                Listener.Prefixes.Add(urlPrefix);
                $"Web server prefix '{urlPrefix}' added.".Info(nameof(WebServer));
            }

            "Finished Loading Web Server.".Info(nameof(WebServer));
        }

        /// <summary>
        /// Gets or sets the on method not allowed.
        /// </summary>
        /// <value>
        /// The on method not allowed.
        /// </value>
        public Func<HttpListenerContext, Task<bool>> OnMethodNotAllowed { get; set; } = ctx =>
             ctx.HtmlResponseAsync(Responses.Response405Html, System.Net.HttpStatusCode.MethodNotAllowed);

        /// <summary>
        /// Gets or sets the on not found.
        /// </summary>
        /// <value>
        /// The on not found.
        /// </value>
        public Func<HttpListenerContext, Task<bool>> OnNotFound { get; set; } = ctx =>
            ctx.HtmlResponseAsync(Responses.Response404Html, System.Net.HttpStatusCode.NotFound);

        /// <summary>
        /// Gets the underlying HTTP listener.
        /// </summary>
        /// <value>
        /// The listener.
        /// </value>
        public HttpListener Listener { get; protected set; }

        /// <summary>
        /// Gets the URL Prefix for which the server is serving requests.
        /// </summary>
        /// <value>
        /// The URL prefix.
        /// </value>
        public HttpListenerPrefixCollection UrlPrefixes => Listener.Prefixes;

        /// <summary>
        /// Gets a list of registered modules.
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();

        /// <summary>
        /// Gets registered the ISessionModule.
        /// </summary>
        /// <value>
        /// The session module.
        /// </value>
        public ISessionWebModule SessionModule { get; protected set; }

        /// <summary>
        /// Gets the URL RoutingStrategy used in this instance.
        /// By default it is set to Wildcard, but Regex is the recommended value.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; protected set; }

        /// <summary>
        /// Static method to create webserver instance.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix) => new WebServer(urlPrefix);

        /// <summary>
        /// Static method to create webserver instance.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="routingStrategy">Matching/Parsing of URL: choose from: Wildcard, Regex, Simple. </param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix, RoutingStrategy routingStrategy) => new WebServer(urlPrefix, routingStrategy);

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <typeparam name="T">The type of module.</typeparam>
        /// <returns>Module registered for the given type.</returns>
        public T Module<T>()
            where T : class, IWebModule
        {
            return Module(typeof(T)) as T;
        }

        /// <summary>
        /// Registers an instance of a web module. Only 1 instance per type is allowed.
        /// </summary>
        /// <param name="module">The module.</param>
        public void RegisterModule(IWebModule module)
        {
            if (module == null) return;
            var existingModule = Module(module.GetType());

            if (existingModule == null)
            {
                module.Server = this;
                _modules.Add(module);

                if (module is ISessionWebModule webModule)
                    SessionModule = webModule;
            }
            else
            {
                $"Failed to register module '{module.GetType()}' because a module with the same type already exists."
                    .Warn(nameof(WebServer));
            }
        }

        /// <summary>
        /// Unregisters the module identified by its type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        public void UnregisterModule(Type moduleType)
        {
            var existingModule = Module(moduleType);

            if (existingModule == null)
            {
                $"Failed to unregister module '{moduleType}' because no module with that type has been previously registered."
                    .Warn(nameof(WebServer));

                return;
            }

            var module = Module(moduleType);
            _modules.Remove(module);

            if (module == SessionModule)
                SessionModule = null;
        }

        /// <summary>
        /// Starts the listener and the registered modules.
        /// </summary>
        /// <param name="ct">The cancellation token; when cancelled, the server cancels all pending requests and stops.</param>
        /// <returns>
        /// Returns the task that the HTTP listener is running inside of, so that it can be waited upon after it's been canceled.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The method was already called.</exception>
        /// <exception cref="System.OperationCanceledException">Cancellation was requested.</exception>
        /// <remarks>
        /// Both the server and client requests are queued separately on the thread pool,
        /// so it is safe to call <see cref="Task.Wait()" /> in a synchronous method.
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
                        var clientSocket = await Listener.GetContextAsync().ConfigureAwait(false);
                        if (ct.IsCancellationRequested)
                            return;

                        // Spawn off each client task asynchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        var handler = new HttpHandler(clientSocket, this);
                        handler.HandleClientRequest(ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                    catch (HttpListenerException)
                    {
                        if (!ct.IsCancellationRequested)
                            throw;
                    }
                    catch (OperationCanceledException)
                    {
                        // Forward cancellations out to the caller.
                        throw;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore disposed Listener
                    }
                    catch (Exception ex)
                    {
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Listener == null) return;

            try
            {
                (Listener as IDisposable).Dispose();
            }
            finally
            {
                Listener = null;
            }

            "Listener Closed.".Info(nameof(WebServer));
        }

        private IWebModule Module(Type moduleType) => Modules.FirstOrDefault(m => m.GetType() == moduleType);
    }
}