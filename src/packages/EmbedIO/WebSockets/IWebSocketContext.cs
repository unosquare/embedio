using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Threading;
using EmbedIO.Sessions;

namespace EmbedIO.WebSockets
{
    /// <summary>
    /// Represents the context of a WebSocket connection.
    /// </summary>
    public interface IWebSocketContext
    {
        /// <summary>
        /// Gets a unique identifier for a WebSocket context.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the <see cref="CancellationToken "/> used to cancel operations.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the unique identifier of the opening handshake HTTP context.
        /// </summary>
        string HttpContextId { get; }

        /// <summary>
        /// Gets the session proxy associated with the opening handshake HTTP context.
        /// </summary>
        ISessionProxy Session { get; }

        /// <summary>
        /// Gets the dictionary of data associated with the opening handshake HTTP context.
        /// </summary>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Gets the server IP address and port number to which the opening handshake request is directed.
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the client IP address and port number from which the opening handshake request originated.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>The URI requested by the WebSocket client.</summary>
        Uri RequestUri { get; }

        /// <summary>The HTTP headers that were sent to the server during the opening handshake.</summary>
        NameValueCollection Headers { get; }

        /// <summary>The value of the Origin HTTP header included in the opening handshake.</summary>
        string Origin { get; }

        /// <summary>The value of the SecWebSocketKey HTTP header included in the opening handshake.</summary>
        string WebSocketVersion { get; }

        /// <summary>The list of subprotocols requested by the WebSocket client.</summary>
        IEnumerable<string> RequestedProtocols { get; }

        /// <summary>The accepted subprotocol.</summary>
        string AcceptedProtocol { get; }

        /// <summary>The cookies that were passed to the server during the opening handshake.</summary>
        ICookieCollection Cookies { get; }

        /// <summary>An object used to obtain identity, authentication information, and security roles for the WebSocket client.</summary>
        IPrincipal User { get; }

        /// <summary>Whether the WebSocket client is authenticated.</summary>
        bool IsAuthenticated { get; }

        /// <summary>Whether the WebSocket client connected from the local machine.</summary>
        bool IsLocal { get; }

        /// <summary>Whether the WebSocket connection is secured using Secure Sockets Layer (SSL).</summary>
        bool IsSecureConnection { get; }

        /// <summary>The <see cref="IWebSocket"/> interface used to interact with the WebSocket connection.</summary>
        IWebSocket WebSocket { get; }
    }
}