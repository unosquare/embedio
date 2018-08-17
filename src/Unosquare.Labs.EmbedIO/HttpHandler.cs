namespace Unosquare.Labs.EmbedIO
{
    using System.Linq;
    using Constants;
    using System.Threading;
    using System.Reflection;
    using Swan;
    using System;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    internal class HttpHandler
    {
        private readonly HttpListenerContext _context;
        private readonly IWebServer _server;
        private string _requestId = "(not set)";

        public HttpHandler(HttpListenerContext context, IWebServer server)
        {
            _context = context;
            _server = server;
        }

        /// <summary>
        /// Handles the client request.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous of client request.</returns>
        public async Task HandleClientRequest(CancellationToken ct)
        {
            try
            {
                // Create a request endpoint string
                var requestEndpoint =
                    $"{_context.Request?.RemoteEndPoint?.Address}:{_context.Request?.RemoteEndPoint?.Port}";

                // Generate a random request ID. It's currently not important but could be useful in the future.
                _requestId = string.Concat(DateTime.Now.Ticks.ToString(), requestEndpoint).GetHashCode().ToString("x2");

                // Log the request and its ID
                $"Start of Request {_requestId} - Source {requestEndpoint} - {_context.RequestVerb().ToString().ToUpperInvariant()}: {_context.Request.Url.PathAndQuery} - {_context.Request.UserAgent}"
                    .Debug(nameof(HttpHandler));

                var processResult = await ProcessRequest(ct);

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (processResult == false)
                {
                    "No module generated a response. Sending 404 - Not Found".Error(nameof(HttpHandler));

                    await _server.OnNotFound(_context);
                }
            }
            catch (Exception ex)
            {
                ex.Log(nameof(HttpHandler), "Error handling request.");
            }
            finally
            {
                // Always close the response stream no matter what.
                _context?.Response.OutputStream.Close();
                $"End of Request {_requestId}".Debug(nameof(HttpHandler));
            }
        }
        
        /// <summary>
        /// Process HttpListener Request and returns <c>true</c>  if it was handled.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns><c>true</c> if it was handled; otherwise, <c>false</c>.</returns>
        public async Task<bool> ProcessRequest(CancellationToken ct)
        {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var module in _server.Modules)
            {
                var callback = GetHandler(module);

                if (callback == null) continue;

                try
                {
                    // Log the module and handler to be called and invoke as a callback.
                    $"{module.Name}::{callback.GetMethodInfo().DeclaringType?.Name}.{callback.GetMethodInfo().Name}"
                        .Debug(nameof(HttpHandler));

                    // Execute the callback
                    var handleResult = await callback(_context, ct);

                    $"Result: {handleResult}".Trace(nameof(HttpHandler));

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (_context.Response.StatusCode != (int)System.Net.HttpStatusCode.Unauthorized)
                    {
                        await ResponseServerError(ct, ex, module.Name);
                    }

                    // Finally set the handled flag to true and exit.
                    return true;
                }
            }

            return false;
        }

        private Task ResponseServerError(CancellationToken ct, Exception ex, string module)
        {
            var priorMessage = $"Failing module name: {module}";
            var errorMessage = ex.ExceptionMessage(priorMessage);

            // Log the exception message.
            ex.Log(nameof(HttpHandler), priorMessage);

            // Send the response over with the corresponding status code.
            return _context.HtmlResponseAsync(
                System.Net.WebUtility.HtmlEncode(string.Format(Responses.Response500HtmlFormat,
                    errorMessage,
                    ex.StackTrace)),
                System.Net.HttpStatusCode.InternalServerError,
                ct);
        }

        private Map GetHandlerFromPath(IWebModule module)
            => module.Handlers.FirstOrDefault(x =>
                string.Equals(
                    x.Path,
                    x.Path == ModuleMap.AnyPath ? ModuleMap.AnyPath : _context.RequestPath(),
                    StringComparison.OrdinalIgnoreCase) &&
                x.Verb == (x.Verb == HttpVerbs.Any ? HttpVerbs.Any : _context.RequestVerb()));

        private Map GetHandlerFromRegexPath(IWebModule module)
            => module.Handlers.FirstOrDefault(x =>
                (x.Path == ModuleMap.AnyPath || _context.RequestRegexUrlParams(x.Path) != null) &&
                (x.Verb == HttpVerbs.Any || x.Verb == _context.RequestVerb()));

        private Map GetHandlerFromWildcardPath(IWebModule module)
        {
            var path = _context.RequestWilcardPath(module.Handlers
                .Where(k => k.Path.Contains("/" + ModuleMap.AnyPath))
                .Select(s => s.Path.ToLowerInvariant()));

            return module.Handlers
                .FirstOrDefault(x =>
                (x.Path == ModuleMap.AnyPath || x.Path == path) &&
                (x.Verb == HttpVerbs.Any || x.Verb == _context.RequestVerb()));
        }

        private Func<HttpListenerContext, CancellationToken, Task<bool>> GetHandler(IWebModule module)
        {
            Map handler;

            switch (_server.RoutingStrategy)
            {
                case RoutingStrategy.Wildcard:
                    handler = GetHandlerFromWildcardPath(module);
                    break;
                case RoutingStrategy.Regex:
                    handler = GetHandlerFromRegexPath(module);
                    break;
                case RoutingStrategy.Simple:
                    handler = GetHandlerFromPath(module);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(RoutingStrategy));
            }

            return handler?.ResponseHandler;
        }
    }
}