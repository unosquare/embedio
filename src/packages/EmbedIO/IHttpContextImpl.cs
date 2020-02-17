using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Sessions;
using EmbedIO.Utilities;
using EmbedIO.WebSockets;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents a HTTP context implementation, i.e. a HTTP context as seen internally by EmbedIO.</para>
    /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended to be used directly from your code,
    /// unless to address specific needs in the implementation of EmbedIO plug-ins (e.g. modules).</para>
    /// </summary>
    /// <seealso cref="IHttpContext" />
    public interface IHttpContextImpl : IHttpContext
    {
        /// <summary>
        /// <para>Gets or sets a <see cref="CancellationToken" /> used to stop processing of this context.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        new CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the route matched by the requested URL path.
        /// </summary>
        new RouteMatch Route { get; set; }

        /// <summary>
        /// <para>Gets or sets the session proxy associated with this context.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <value>
        /// An <see cref="ISessionProxy"/> interface.
        /// </value>
        new ISessionProxy Session { get; set; }

        /// <summary>
        /// <para>Gets or sets the user.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        new IPrincipal User { get; set; }

        /// <summary>
        /// <para>Gets or sets a value indicating whether compressed request bodies are supported.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <seealso cref="WebServerOptionsBase.SupportCompressedRequests"/>
        new bool SupportCompressedRequests { get; set; }

        /// <summary>
        /// <para>Gets the MIME type providers.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        MimeTypeProviderStack MimeTypeProviders { get; }

        /// <summary>
        /// <para>Flushes and closes the response stream, then calls any registered close callbacks.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <seealso cref="IHttpContext.OnClose"/>
        void Close();

        /// <summary>
        /// <para>Asynchronously handles a WebSockets opening handshake
        /// and returns a newly-created <seealso cref="IWebSocketContext"/> interface.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="requestedProtocols">The requested WebSocket sub-protocols.</param>
        /// <param name="acceptedProtocol">The accepted WebSocket sub-protocol,
        /// or the empty string is no sub-protocol has been agreed upon.</param>
        /// <param name="receiveBufferSize">Size of the receive buffer.</param>
        /// <param name="keepAliveInterval">The keep-alive interval.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the server.</param>
        /// <returns>
        /// An <see cref="IWebSocketContext"/> interface.
        /// </returns>
        Task<IWebSocketContext> AcceptWebSocketAsync(
            IEnumerable<string> requestedProtocols, 
            string acceptedProtocol, 
            int receiveBufferSize, 
            TimeSpan keepAliveInterval,
            CancellationToken cancellationToken);
    }
}