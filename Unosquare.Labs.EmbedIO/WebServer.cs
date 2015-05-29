namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Log;

    /// <summary>
    /// Represents our tiny web server used to handle requests
    /// </summary>
    public class WebServer : IDisposable
    {
        private readonly List<IWebModule> _modules = new List<IWebModule>(4);
        private Task _listenerTask;

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
        public HttpListenerPrefixCollection UrlPrefixes
        {
            get { return this.Listener.Prefixes; }
        }

        /// <summary>
        /// Gets a list of regitered modules
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        public ReadOnlyCollection<IWebModule> Modules
        {
            get { return _modules.AsReadOnly(); }
        }

        /// <summary>
        /// Gets registered the ISessionModule.
        /// </summary>
        /// <value>
        /// The session module.
        /// </value>
        public ISessionWebModule SessionModule { get; protected set; }

        /// <summary>
        /// Gets the log interface to which this instance will log messages.
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        public ILog Log { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        public WebServer()
            : this("http://*/", new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port)
            : this(port, new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log"></param>
        public WebServer(int port, ILog log)
            : this("http://*:" + port.ToString() + "/", log)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServer(string urlPrefix)
            : this(urlPrefix, new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        public WebServer(string urlPrefix, ILog log)
            : this(new[] {urlPrefix}, log)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        public WebServer(string[] urlPrefixes)
            : this(urlPrefixes, new NullLog())
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="log">The Log component</param>
        /// <exception cref="System.InvalidOperationException">The HTTP Listener is not supported in this OS</exception>
        /// <exception cref="System.ArgumentException">Argument urlPrefix must be specified</exception>
        public WebServer(string[] urlPrefixes, ILog log)
        {
            if (HttpListener.IsSupported == false)
                throw new InvalidOperationException("The HTTP Listener is not supported in this OS");

            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            if (log == null)
                throw new ArgumentException("Argument log must be specified");

            this.Listener = new HttpListener();
            this.Log = log;

            foreach (var prefix in urlPrefixes)
            {
                var urlPrefix = prefix.Clone() as string;
                if (urlPrefix.EndsWith("/") == false) urlPrefix = urlPrefix + "/";
                urlPrefix = urlPrefix.ToLowerInvariant();

                this.Listener.Prefixes.Add(urlPrefix);
                this.Log.InfoFormat("Web server prefix '{0}' added.", urlPrefix);
            }

            this.Log.Info("Finished Loading Web Server.");
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
            var module = this.Modules.FirstOrDefault(m => m.GetType() == typeof (T));
            if (module != null) return module as T;
            return null;
        }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        /// <returns></returns>
        private IWebModule Module(Type moduleType)
        {
            return Modules.FirstOrDefault(m => m.GetType() == moduleType);
        }

        /// <summary>
        /// Registers an instance of a web module. Only 1 instance per type is allowed.
        /// </summary>
        /// <param name="module">The module.</param>
        public void RegisterModule(IWebModule module)
        {
            if (module == null) return;
            var existingModule = this.Module(module.GetType());
            if (existingModule == null)
            {
                module.Server = this;
                this._modules.Add(module);

                if (module as ISessionWebModule != null)
                    this.SessionModule = module as ISessionWebModule;
            }
            else
            {
                Log.WarnFormat("Failed to register module '{0}' because a module with the same type already exists.",
                    module.GetType());
            }
        }

        /// <summary>
        /// Unregisters the module identified by its type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        public void UnregisterModule(Type moduleType)
        {
            var existingModule = this.Module(moduleType);
            if (existingModule == null)
            {
                Log.WarnFormat(
                    "Failed to unregister module '{0}' because no module with that type has been previously registered.",
                    moduleType);
            }
            else
            {
                var module = this.Module(moduleType);
                this._modules.Remove(module);
                if (module == SessionModule)
                    SessionModule = null;
            }
        }

        /// <summary>
        /// Handles the client request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="app"></param>
        private async void HandleClientRequest(HttpListenerContext context, Middleware app)
        {
            // start with an empty request ID
            var requestId = "(not set)";

            try
            {
                // Generate a MiddlewareContext and expected the result
                if (app != null)
                {
                    var middlewareContext = new MiddlewareContext(context, this);
                    await app.Invoke(middlewareContext);

                    if (middlewareContext.Handled) return;
                }

                // Create a request endpoint string
                var requestEndpoint = string.Join(":",
                    context.Request.RemoteEndPoint.Address.ToString(),
                    context.Request.RemoteEndPoint.Port.ToString(CultureInfo.InvariantCulture));

                // Generate a random request ID. It's currently not important butit could be useful in the future.
                requestId = string.Concat(DateTime.Now.Ticks.ToString(), requestEndpoint).GetHashCode().ToString("x2");

                // Log the request and its ID
                Log.DebugFormat("Start of Request {0}", requestId);
                Log.DebugFormat("Source {0} - {1}: {2}",
                    requestEndpoint,
                    context.RequestVerb().ToString().ToUpperInvariant(),
                    context.RequestPath());

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (ProcessRequest(context) == false)
                {
                    Log.Error("No module generated a response. Sending 404 - Not Found");
                    var responseBytes = System.Text.Encoding.UTF8.GetBytes(Constants.Response404Html);
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error handling request.", ex);
            }
            finally
            {
                // Always close the response stream no matter what.
                context.Response.OutputStream.Close();
                Log.DebugFormat("End of Request {0}\r\n", requestId);
            }
        }

        /// <summary>
        /// Process HttpListener Request and returns true if it was handled
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        public bool ProcessRequest(HttpListenerContext context)
        {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var module in this.Modules)
            {
                // Establish the handler
                var handler = module.Handlers.FirstOrDefault(x =>
                    x.Path == (x.Path == ModuleMap.AnyPath ? ModuleMap.AnyPath : context.RequestPath()) &&
                    x.Verb == (x.Verb == HttpVerbs.Any ? HttpVerbs.Any : context.RequestVerb()));

                if (handler == null || handler.ResponseHandler == null)
                    continue;

                // Establish the callback
                var callback = handler.ResponseHandler;

                try
                {
                    // Inject the Server property of the module via reflection if not already there. (mini IoC ;))
                    if (module.Server == null)
                        module.Server = this;

                    // Log the module and hanlder to be called and invoke as a callback.
                    Log.DebugFormat("{0}::{1}.{2}", module.Name, callback.Method.DeclaringType.Name,
                        callback.Method.Name);

                    // Execute the callback
                    var handleResult = callback.Invoke(this, context);
                    Log.DebugFormat("Result: {0}", handleResult.ToString());

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (context.Response.StatusCode != (int) HttpStatusCode.Unauthorized)
                    {
                        // Log the exception message.
                        var errorMessage = ex.ExceptionMessage("Failing module name: " + module.Name);
                        Log.Error(errorMessage, ex);

                        // Generate an HTML response
                        var response = String.Format(Constants.Response500HtmlFormat,
                            WebUtility.HtmlEncode(errorMessage),
                            WebUtility.HtmlEncode(ex.StackTrace));

                        // Send the response over with the corresponding status code.
                        var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
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
        /// <exception cref="System.InvalidOperationException">The method was already called.</exception>
        public void RunAsync(CancellationToken ct = default(CancellationToken), Middleware app = null)
        {
            if (_listenerTask != null)
                throw new InvalidOperationException("The method was already called.");

            this.Listener.IgnoreWriteExceptions = true;
            this.Listener.Start();

            this.Log.Info("Started HTTP Listener");
            this._listenerTask = Task.Factory.StartNew(async () =>
            {
                while (this.Listener != null && this.Listener.IsListening)
                {
                    try
                    {
                        var clientSocket = await Listener.GetContextAsync();
                        var clientTask =
                            Task.Factory.StartNew((context) => HandleClientRequest(context as HttpListenerContext, app),
                                clientSocket, ct);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }, ct);
        }

        /// <summary>
        /// Starts the listener and the registered modules
        /// </summary>
        [Obsolete("Use the RunAsync method instead.", true)]
        public void Run()
        {
            this.Listener.IgnoreWriteExceptions = true;
            this.Listener.Start();

            this.Log.Info("Started HTTP Listener");

            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (this.Listener != null && this.Listener.IsListening)
                {
                    try
                    {
                        // Asynchrounously queue a response by using a thread from the thread pool
                        ThreadPool.QueueUserWorkItem((contextState) =>
                        {
                            // get a reference to the HTTP Listener Context
                            var context = contextState as HttpListenerContext;
                            this.HandleClientRequest(context, null);
                        }, this.Listener.GetContext());
                        // Retrieve and pass the listener context to the threadpool thread.
                    }
                    catch
                    {
                        // swallow IO exceptions
                    }
                }
            }, this.Listener); // Retrieve and pass the HTTP Listener
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
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
            if (this.Listener != null)
            {
                this.Listener.Stop();
                this.Listener.Close();
                this.Listener = null;
                Log.Info("Listener Closed.");
            }

            if (_listenerTask != null)
            {
                _listenerTask.Dispose();
            }
        }

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(string urlPrefix, ILog log = null)
        {
            return new WebServer(urlPrefix, log ?? new NullLog());
        }

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create(int port, ILog log = null)
        {
            return new WebServer(port, log ?? new NullLog());
        }

        /// <summary>
        /// Static method to create webser instance with SimpleConsoleLog
        /// </summary>
        /// <param name="urlPrefix"></param>
        /// <returns>The webserver instance.</returns>
        public static WebServer CreateWithConsole(string urlPrefix)
        {
            return new WebServer(urlPrefix, new SimpleConsoleLog());
        }
    }
}