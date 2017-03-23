namespace Unosquare.Labs.EmbedIO
{
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;
#if NET46
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents our tiny web server used to handle requests
    /// </summary>
    public class WebServer : IDisposable
    {
        private readonly List<IWebModule> _modules = new List<IWebModule>(4);
        
        /// <summary>
        /// Gets the underlying HTTP listener.
        /// </summary>
        /// <value>
        /// The listener.
        /// </value>
        public HttpListener Listener { get; protected set; }

        /// <summary>
        /// Gets the Url Prefix for which the server is serving requests.
        /// </summary>
        /// <value>
        /// The URL prefix.
        /// </value>
        public HttpListenerPrefixCollection UrlPrefixes => Listener.Prefixes;

        /// <summary>
        /// Gets a list of registered modules
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
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        public WebServer()
            : this(new[] { "http://*/" }, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port)
            : this(new[] { "http://*:" + port + "/" }, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer" /> class.
        /// This constructor does not provide any Logging capabilities.
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
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        public WebServer(string[] urlPrefixes)
            : this(urlPrefixes, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy</param>
        /// <exception cref="System.InvalidOperationException">The HTTP Listener is not supported in this OS</exception>
        /// <exception cref="System.ArgumentException">Argument urlPrefix must be specified</exception>
        public WebServer(string[] urlPrefixes, RoutingStrategy routingStrategy)
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
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Module<T>()
            where T : class, IWebModule
        {
            return Module(typeof(T)) as T;
        }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        /// <returns></returns>
        private IWebModule Module(Type moduleType) => Modules.FirstOrDefault(m => m.GetType() == moduleType);

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

                var webModule = module as ISessionWebModule;

                if (webModule != null)
                    SessionModule = webModule;
            }
            else
            {
                $"Failed to register module '{module.GetType()}' because a module with the same type already exists.".Warn(nameof(WebServer));
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
                $"Failed to unregister module '{moduleType}' because no module with that type has been previously registered.".Warn(nameof(WebServer));
            }
            else
            {
                var module = Module(moduleType);
                _modules.Remove(module);

                if (module == SessionModule)
                    SessionModule = null;
            }
        }

        /// <summary>
        /// Handles the client request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        private async Task HandleClientRequest(HttpListenerContext context, CancellationToken ct)
        {
            // start with an empty request ID
            var requestId = "(not set)";

            try
            {
                // Create a request endpoint string
                var requestEndpoint = $"{context.Request?.RemoteEndPoint?.Address}:{context.Request?.RemoteEndPoint?.Port}";

                // Generate a random request ID. It's currently not important but could be useful in the future.
                requestId = string.Concat(DateTime.Now.Ticks.ToString(), requestEndpoint).GetHashCode().ToString("x2");

                // Log the request and its ID
                $"Start of Request {requestId}".Debug(nameof(WebServer));
                $"Source {requestEndpoint} - {context.RequestVerb().ToString().ToUpperInvariant()}: {context.RequestPath()}".Debug(nameof(WebServer));

                var processResult = await ProcessRequest(context, ct);

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (processResult == false)
                {
                    "No module generated a response. Sending 404 - Not Found".Error();
                    var responseBytes = System.Text.Encoding.UTF8.GetBytes(Constants.Response404Html);
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                    await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length, ct);
                }
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebServer), "Error handling request.");
            }
            finally
            {
                // Always close the response stream no matter what.
#if NET46
                context?.Response.OutputStream.Close();
#else
                await context.Response.OutputStream.CloseAsync();
#endif
                $"End of Request {requestId}".Debug(nameof(WebServer));
            }
        }

        /// <summary>
        /// Process HttpListener Request and returns true if it was handled
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
        public async Task<bool> ProcessRequest(HttpListenerContext context, CancellationToken ct)
        {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var module in Modules)
            {
                // Establish the handler
                var handler = module.Handlers.FirstOrDefault(x =>
                    string.Equals(x.Path, x.Path == ModuleMap.AnyPath ? ModuleMap.AnyPath : context.RequestPath(), StringComparison.OrdinalIgnoreCase) &&
                    x.Verb == (x.Verb == HttpVerbs.Any ? HttpVerbs.Any : context.RequestVerb()));

                if (handler?.ResponseHandler == null)
                    continue;

                // Establish the callback
                var callback = handler.ResponseHandler;

                try
                {
                    // Inject the Server property of the module via reflection if not already there. (mini IoC ;))
                    if (module.Server == null)
                        module.Server = this;

                    // Log the module and handler to be called and invoke as a callback.
                    $"{module.Name}::{callback.GetMethodInfo().DeclaringType?.Name}.{callback.GetMethodInfo().Name}".Debug(nameof(WebServer));

                    // Execute the callback
                    var handleResult = await callback(context, ct);

                    $"Result: {handleResult}".Trace(nameof(WebServer));

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (context.Response.StatusCode != (int)System.Net.HttpStatusCode.Unauthorized)
                    {
                        var errorMessage = ex.ExceptionMessage("Failing module name: " + module.Name);

                        // Log the exception message.
                        ex.Log(nameof(WebServer), $"Failing module name: {module.Name}");

                        // Generate an HTML response
                        var response = System.Net.WebUtility.HtmlEncode(string.Format(Constants.Response500HtmlFormat,
                            errorMessage, ex.StackTrace));

                        // Send the response over with the corresponding status code.
                        var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    // Finally set the handled flag to true and exit.
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts the listener and the registered modules
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
        public async Task RunAsync(CancellationToken ct = default(CancellationToken))
        {
            Listener.IgnoreWriteExceptions = true;
            Listener.Start();

            "Started HTTP Listener".Info(nameof(WebServer));

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
                    HandleClientRequest(clientSocket, ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                catch (OperationCanceledException)
                {
                    // Forward cancellations out to the caller.
                    throw;
                }
                catch (Exception ex)
                {
                    ex.Log(nameof(WebServer));
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // free managed resources
            if (Listener == null) return;

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

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix) => new WebServer(urlPrefix);
    }
}