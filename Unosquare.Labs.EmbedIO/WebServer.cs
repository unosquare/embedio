﻿namespace Unosquare.Labs.EmbedIO {
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
    public class WebServer : IDisposable {
        private readonly List<IWebModule> _modules = new List<IWebModule> (4);
        private readonly List<string> _urlRoots = new List<string> ();
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
        public HttpListenerPrefixCollection UrlPrefixes {
            get { return this.Listener.Prefixes; }
        }

        /// <summary>
        /// Gets a list of regitered modules
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        public ReadOnlyCollection<IWebModule> Modules {
            get { return _modules.AsReadOnly (); }
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
        /// Gets the URL RoutingStrategy used in this instance.
        /// By default it is set to Wildcard, but Regex is the the recommended value.
        /// </summary>
        public RoutingStrategy RoutingStrategy { get; protected set; }

        /// <summary>
        /// Gets Url part after the port on which the server is listening (every item correspond to Url prefix).
        /// </summary>
        public ReadOnlyCollection<string> UrlRoots { get { return _urlRoots.AsReadOnly (); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        public WebServer ()
            : this ("http://*/", new NullLog (), RoutingStrategy.Wildcard) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer (int port)
            : this (port, new NullLog ()) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log"></param>
        public WebServer (int port, ILog log)
            : this ("http://*:" + port.ToString () + "/", log, RoutingStrategy.Wildcard) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log"></param>
        /// <param name="routingStrategy">The routing strategy</param>
        public WebServer (int port, ILog log, RoutingStrategy routingStrategy)
            : this ("http://*:" + port.ToString () + "/", log, routingStrategy) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        public WebServer (string urlPrefix)
            : this (urlPrefix, new NullLog (), RoutingStrategy.Wildcard) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        public WebServer (string urlPrefix, ILog log)
            : this (new[] { urlPrefix }, log, RoutingStrategy.Wildcard) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        /// <param name="routingStrategy">The routing strategy</param>
        public WebServer (string urlPrefix, ILog log, RoutingStrategy routingStrategy)
            : this (new[] { urlPrefix }, log, routingStrategy) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// This constructor does not provide any Logging capabilities.
        /// </summary>
        /// <param name="urlPrefixes">The URL prefixes.</param>
        public WebServer (string[] urlPrefixes)
            : this (urlPrefixes, new NullLog (), RoutingStrategy.Wildcard) {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash. -- It is important
        /// </summary>
        /// <param name="urlPrefixes">The URL prefix.</param>
        /// <param name="log">The Log component</param>
        /// <param name="routingStrategy">The routing strategy</param>
        /// <exception cref="System.InvalidOperationException">The HTTP Listener is not supported in this OS</exception>
        /// <exception cref="System.ArgumentException">Argument urlPrefix must be specified</exception>
        public WebServer (string[] urlPrefixes, ILog log, RoutingStrategy routingStrategy) {
            if (HttpListener.IsSupported == false)
                throw new InvalidOperationException ("The HTTP Listener is not supported in this OS");

            if (urlPrefixes == null || urlPrefixes.Length <= 0)
                throw new ArgumentException ("At least 1 URL prefix in urlPrefixes must be specified");

            if (log == null)
                throw new ArgumentException ("Argument log must be specified");

            this.RoutingStrategy = routingStrategy;
            this.Listener = new HttpListener ();
            this.Log = log;

            foreach (var prefix in urlPrefixes) {
                var urlPrefix = prefix.Clone () as string;
                if (urlPrefix.EndsWith ("/") == false) urlPrefix = urlPrefix + "/";
                urlPrefix = urlPrefix.ToLowerInvariant ();
                this._urlRoots.Add (UrlPrefixToUrlRoot (urlPrefix));
                this.Listener.Prefixes.Add (urlPrefix);
                this.Log.InfoFormat ("Web server prefix '{0}' added.", urlPrefix);
            }

            this.Log.Info ("Finished Loading Web Server.");
        }

        /// <summary>
        /// maps HttpListener Prefix to url root, which is part after port and third slash
        /// </summary>
        /// <returns></returns>
        protected string UrlPrefixToUrlRoot (string prefix) {
            var countOfSlashes = 3;
            var slashesFound = 0;
            int i;
            for (i = 0; i < prefix.Length; i++) {
                if (prefix[i] == '/') slashesFound++;
                if (slashesFound == countOfSlashes) break;
            }
            if (slashesFound != countOfSlashes) {
                //misformated prefix?
                return "/";
            } else {
                return new string (prefix.Skip (i).ToArray ()).ToLowerInvariant ();
            }
        }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Module<T> ()
            where T : class, IWebModule {
            var module = this.Modules.FirstOrDefault (m => m.GetType () == typeof (T));
            return module as T;
        }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        /// <returns></returns>
        private IWebModule Module (Type moduleType) {
            return Modules.FirstOrDefault (m => m.GetType () == moduleType);
        }

        /// <summary>
        /// Registers an instance of a web module. Only 1 instance per type is allowed.
        /// </summary>
        /// <param name="module">The module.</param>
        public void RegisterModule (IWebModule module) {
            if (module == null) return;
            var existingModule = this.Module (module.GetType ());
            if (existingModule == null) {
                module.Server = this;
                this._modules.Add (module);

                if (module is ISessionWebModule)
                    this.SessionModule = module as ISessionWebModule;
            } else {
                Log.WarnFormat ("Failed to register module '{0}' because a module with the same type already exists.",
                    module.GetType ());
            }
        }

        /// <summary>
        /// Unregisters the module identified by its type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        public void UnregisterModule (Type moduleType) {
            var existingModule = this.Module (moduleType);
            if (existingModule == null) {
                Log.WarnFormat (
                    "Failed to unregister module '{0}' because no module with that type has been previously registered.",
                    moduleType);
            } else {
                var module = this.Module (moduleType);
                this._modules.Remove (module);
                if (module == SessionModule)
                    SessionModule = null;
            }
        }

        /// <summary>
        /// Handles the client request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="app"></param>
        private async void HandleClientRequest (HttpListenerContext context, Middleware app) {
            // start with an empty request ID
            var requestId = "(not set)";

            try {
                // Generate a MiddlewareContext and expected the result
                if (app != null) {
                    var middlewareContext = new MiddlewareContext (context, this);
                    await app.Invoke (middlewareContext);

                    if (middlewareContext.Handled) return;
                }

                // Create a request endpoint string
                var requestEndpoint = string.Join (":",
                    context.Request.RemoteEndPoint.Address.ToString (),
                    context.Request.RemoteEndPoint.Port.ToString (CultureInfo.InvariantCulture));

                // Generate a random request ID. It's currently not important butit could be useful in the future.
                requestId = string.Concat (DateTime.Now.Ticks.ToString (), requestEndpoint).GetHashCode ().ToString ("x2");

                // Log the request and its ID
                Log.DebugFormat ("Start of Request {0}", requestId);
                Log.DebugFormat ("Source {0} - {1}: {2}",
                    requestEndpoint,
                    context.RequestVerb ().ToString ().ToUpperInvariant (),
                    context.RequestPath ());

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (ProcessRequest (context) == false) {
                    Log.Error ("No module generated a response. Sending 404 - Not Found");
                    var responseBytes = System.Text.Encoding.UTF8.GetBytes (Constants.Response404Html);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.OutputStream.Write (responseBytes, 0, responseBytes.Length);
                }
            } catch (Exception ex) {
                Log.Error ("Error handling request.", ex);
            } finally {
                // Always close the response stream no matter what.
                context.Response.OutputStream.Close ();
                Log.DebugFormat ("End of Request {0}\r\n", requestId);
            }
        }

        /// <summary>
        /// Process HttpListener Request and returns true if it was handled
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        public bool ProcessRequest (HttpListenerContext context) {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var module in this.Modules) {
                // Establish the handler
                var handler = module.Handlers.FirstOrDefault (x =>
                     x.Path == (x.Path == ModuleMap.AnyPath ? ModuleMap.AnyPath : context.RequestPath ()) &&
                     x.Verb == (x.Verb == HttpVerbs.Any ? HttpVerbs.Any : context.RequestVerb ()));

                if (handler == null || handler.ResponseHandler == null)
                    continue;

                // Establish the callback
                var callback = handler.ResponseHandler;

                try {
                    // Inject the Server property of the module via reflection if not already there. (mini IoC ;))
                    if (module.Server == null)
                        module.Server = this;

                    // Log the module and hanlder to be called and invoke as a callback.
                    Log.DebugFormat ("{0}::{1}.{2}", module.Name, callback.Method.DeclaringType.Name,
                        callback.Method.Name);

                    // Execute the callback
                    var handleResult = callback.Invoke (this, context);
                    Log.DebugFormat ("Result: {0}", handleResult.ToString ());

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult) {
                        return true;
                    }
                } catch (Exception ex) {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (context.Response.StatusCode != (int)HttpStatusCode.Unauthorized) {
                        // Log the exception message.
                        var errorMessage = ex.ExceptionMessage ("Failing module name: " + module.Name);
                        Log.Error (errorMessage, ex);

                        // Generate an HTML response
                        var response = String.Format (Constants.Response500HtmlFormat,
                            WebUtility.HtmlEncode (errorMessage),
                            WebUtility.HtmlEncode (ex.StackTrace));

                        // Send the response over with the corresponding status code.
                        var responseBytes = System.Text.Encoding.UTF8.GetBytes (response);
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.OutputStream.Write (responseBytes, 0, responseBytes.Length);
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
        /// <returns>
        /// Returns the task that the HTTP listener is 
        /// running inside of, so that it can be waited upon after it's been canceled.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The method was already called.</exception>
        public Task RunAsync (CancellationToken ct = default (CancellationToken), Middleware app = null) {
            if (_listenerTask != null)
                throw new InvalidOperationException ("The method was already called.");

            this.Listener.IgnoreWriteExceptions = true;
            this.Listener.Start ();

            this.Log.Info ("Started HTTP Listener");
            this._listenerTask = Task.Factory.StartNew (() => {
                while (this.Listener != null && this.Listener.IsListening) {
                    try {
                        var clientSocketTask = Listener.GetContextAsync ();
                        clientSocketTask.Wait (ct);
                        var clientSocket = clientSocketTask.Result;

                        var clientTask =
                            Task.Factory.StartNew ((context) => HandleClientRequest (context as HttpListenerContext, app),
                                clientSocket, ct);
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        Log.Error (ex);
                    }
                }
            }, ct);
            return this._listenerTask;
        }

        /// <summary>
        /// Starts the listener and the registered modules
        /// </summary>
        [Obsolete ("Use the RunAsync method instead.", true)]
        public void Run () {
            this.Listener.IgnoreWriteExceptions = true;
            this.Listener.Start ();

            this.Log.Info ("Started HTTP Listener");

            ThreadPool.QueueUserWorkItem ((o) => {
                while (this.Listener != null && this.Listener.IsListening) {
                    try {
                        // Asynchrounously queue a response by using a thread from the thread pool
                        ThreadPool.QueueUserWorkItem ((contextState) => {
                            // get a reference to the HTTP Listener Context
                            var context = contextState as HttpListenerContext;
                            this.HandleClientRequest (context, null);
                        }, this.Listener.GetContext ());
                        // Retrieve and pass the listener context to the threadpool thread.
                    } catch {
                        // swallow IO exceptions
                    }
                }
            }, this.Listener); // Retrieve and pass the HTTP Listener
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose () {
            this.Dispose (true);
            GC.SuppressFinalize (this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose (bool disposing) {
            if (!disposing) return;

            // free managed resources
            if (this.Listener != null) {
                try {
                    (this.Listener as IDisposable).Dispose ();
                } finally {
                    this.Listener = null;
                }

                Log.Info ("Listener Closed.");
            }
        }

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create (string urlPrefix, ILog log = null) {
            return new WebServer (urlPrefix, log ?? new NullLog ());
        }

        /// <summary>
        /// Static method to create webserver instance
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="log">The log.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer Create (int port, ILog log = null) {
            return new WebServer (port, log ?? new NullLog ());
        }

        /// <summary>
        /// Static method to create a webserver instance using a simple console output.
        /// This method is useful for fluent configuration.
        /// </summary>
        /// <param name="urlPrefix"></param>
        /// <returns>The webserver instance.</returns>
        public static WebServer CreateWithConsole (string urlPrefix) {
            return new WebServer (urlPrefix, new SimpleConsoleLog ());
        }

        /// <summary>
        /// Static method to create a webserver instance using a simple console output.
        /// This method is useful for fluent configuration.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        public static WebServer CreateWithConsole (string urlPrefix, RoutingStrategy routingStrategy) {
            return new WebServer (urlPrefix, new SimpleConsoleLog (), routingStrategy);
        }
    }
}