namespace Unosquare.Labs.EmbedIO
{
    using log4net;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;

    /// <summary>
    /// Represents our tiny web server used to handle requests
    /// </summary>
    public class WebServer : IDisposable
    {
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
        public HttpListenerPrefixCollection UrlPrefixes { get { return this.Listener.Prefixes; } }

        /// <summary>
        /// Gets the collection holding the modules
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        public List<WebServerModule> Modules { get; protected set; }

        /// <summary>
        /// Gets the log interface to which this instance will log messages.
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        public ILog Log { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// </summary>
        /// <param name="port">The port.</param>
        public WebServer(int port, ILog log)
            : this("http://*:" + port.ToString() + "/", log)
        {
            // placeholder
        }

        public WebServer(string urlPrefix, ILog log)
            : this(new string[] { urlPrefix }, log)
        {
            // placeholder
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class.
        /// NOTE: urlPrefix must be specified as something similar to: http://localhost:9696/
        /// Please notice the ending slash.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        /// <exception cref="System.InvalidOperationException">The HTTP Listener is not supported in this OS</exception>
        /// <exception cref="System.ArgumentException">Argument urlPrefix must be specified</exception>
        public WebServer(string[] urlPrefixes, ILog log)
        {
            if (HttpListener.IsSupported == false)
                throw new InvalidOperationException("The HTTP Listener is not supported in this OS");

            if (urlPrefixes.Length <= 0)
                throw new ArgumentException("At least 1 URL prefix in urlPrefixes must be specified");

            if (log == null)
                throw new ArgumentException("Argument log must be specified");

            this.Listener = new HttpListener();
            this.Modules = new List<WebServerModule>();
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
            where T : WebServerModule
        {
            var module = this.Modules.FirstOrDefault(m => m.GetType() == typeof(T));
            if (module != null) return module as T;
            return null;
        }

        /// <summary>
        /// Starts the listener and the registered modules
        /// </summary>
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

                            // start with an empty request ID
                            string requestId = "(not set)";

                            try
                            {
                                // Extract path and verb for matching
                                var path = context.RequestPath();
                                var verb = context.RequestVerb();

                                var handled = false;

                                // Create a request endpoint string
                                var requestEndpoint = string.Join(":",
                                    context.Request.RemoteEndPoint.Address.ToString(),
                                    context.Request.RemoteEndPoint.Port.ToString());

                                // Generate a random request ID. It's currently not important butit could be useful in the future.
                                requestId = string.Concat(DateTime.Now.Ticks.ToString(), requestEndpoint).GetHashCode().ToString("x2");

                                // Log the request and its ID
                                Log.DebugFormat("Start of Request {0}", requestId);
                                Log.DebugFormat("Source {0} - {1}: {2}",
                                    requestEndpoint,
                                    verb.ToString().ToUpperInvariant(),
                                    path);

                                // Iterate though the loaded modules to match up a request and possibly generate a response.
                                foreach (var module in this.Modules)
                                {
                                    // First, try to match to an All-Path (*) handler
                                    var isAllPathHandler = module.Handlers.ContainsKey(WebServerModuleMap.AnyPath);
                                    if (module.Handlers.ContainsKey(path) == false && isAllPathHandler == false)
                                        continue;

                                    // Establish the handler
                                    var handler = isAllPathHandler ? module.Handlers[WebServerModuleMap.AnyPath] : module.Handlers[path];

                                    // Paths could also match an All-Verb handler (Any)
                                    var isAllVerbHandler = handler.ContainsKey(HttpVerbs.Any);
                                    if (handler.ContainsKey(verb) == false && isAllVerbHandler == false)
                                        continue;

                                    // Establish the callback
                                    var callback = isAllVerbHandler ? handler[HttpVerbs.Any] : handler[verb];

                                    try
                                    {
                                        // Inject the Server property of the module via reflection if not already there. (mini IoC ;))
                                        if (module.Server == null)
                                            module.GetType().GetProperty("Server").SetValue(module, this, null);

                                        // Log the module and hanlder to be called and invoke as a callback.
                                        Log.DebugFormat("{0}::{1}.{2}", module.Name, callback.Method.DeclaringType.Name, callback.Method.Name);
                                        var handleResult = callback.Invoke(this, context);
                                        Log.DebugFormat("Result: {0}", handleResult.ToString());

                                        // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                                        if (handleResult)
                                        {
                                            handled = true;
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Handle exceptions by returning a 500 (Internal Server Error) 
                                        if (context.Response.StatusCode != 401)
                                        {
                                            // Log the exception message.
                                            var errorMessage = ex.ExceptionMessage("Failing module name: " + module.Name);
                                            Log.Error(errorMessage, ex);

                                            // Generate an HTML response
                                            var response = "<html><head></head><body><h1>500 - Internal Server Error</h1><h2>Message</h2><pre>"
                                                           + System.Net.WebUtility.HtmlEncode(errorMessage)
                                                           + "</pre><h2>Stack Trace</h2><pre>\r\n"
                                                           + System.Net.WebUtility.HtmlEncode(ex.StackTrace) +
                                                           "</pre></body></html>";

                                            // Send the response over with the corresponding status code.
                                            var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                                            context.Response.StatusCode = 500;
                                            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                                        }

                                        // Finally set the handled flag to true and exit.
                                        handled = true;
                                        break;
                                    }
                                }

                                // Return a 404 (Not Found) response if no mudule/handler handled the response.
                                if (!handled)
                                {
                                    Log.Error("No module generated a response. Sending 404 - Not Found");
                                    var response = "<html><head></head><body><h1>404 - Not Found</h1></body></html>";
                                    var responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                                    context.Response.StatusCode = 404;
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

                        }, this.Listener.GetContext()); // Retrieve and pass the listener context to the threadpool thread.
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
            if (disposing)
            {
                // free managed resources
                if (this.Listener != null)
                {
                    this.Listener.Stop();
                    this.Listener.Close();
                    this.Listener = null;
                    Log.Info("Listener Closed.");
                }
            }
        }

    }

}
