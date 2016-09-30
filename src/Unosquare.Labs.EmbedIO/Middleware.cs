namespace Unosquare.Labs.EmbedIO
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a Middleware abstract class
    /// </summary>
    public abstract class Middleware
    {
        /// <summary>
        /// Invokes the Middleware with a context
        /// </summary>
        /// <param name="context">The Middleware context</param>
        public abstract Task Invoke(MiddlewareContext context);
    }

    /// <summary>
    /// A Middlware context
    /// </summary>
    public class MiddlewareContext
    {
        /// <summary>
        /// The Http Context
        /// </summary>
        public HttpListenerContext HttpContext { get; private set; }

        /// <summary>
        /// Flags if the middleware resolves the request
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Web Server instance
        /// </summary>
        public WebServer WebServer { get; private set; }

        /// <summary>
        /// Instances a Middleware context
        /// </summary>
        /// <param name="httpListenerContext">The HttpListenerContext</param>
        /// <param name="webserver">The WebServer instance</param>
        public MiddlewareContext(HttpListenerContext httpListenerContext, WebServer webserver)
        {
            HttpContext = httpListenerContext;
            Handled = false;
            WebServer = webserver;
        }
    }
}