namespace Unosquare.Labs.EmbedIO.Constants
{
    using System;

    /// <summary>
    /// HTTP Header Constants.
    /// </summary>
    [Obsolete("This constants will be available in the new HttpHeaderNames class")]
    public static class HttpHeaders
    {
        /// <summary>
        /// Access-Control-Allow-Origin HTTP Header.
        /// </summary>
        public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

        /// <summary>
        /// Access-Control-Allow-Headers HTTP Header.
        /// </summary>
        public const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";

        /// <summary>
        /// Access-Control-Allow-Methods HTTP Header.
        /// </summary>
        public const string AccessControlAllowMethods = "Access-Control-Allow-Methods";

        /// <summary>
        /// Origin HTTP Header.
        /// </summary>
        public const string Origin = "Origin";

        /// <summary>
        /// Access-Control-Request-Headers HTTP Header.
        /// </summary>
        public const string AccessControlRequestHeaders = "Access-Control-Request-Headers";

        /// <summary>
        /// Access-Control-Request-Headers HTTP Method.
        /// </summary>
        public const string AccessControlRequestMethod = "Access-Control-Request-Method";
        
        /// <summary>
        /// The cookie header.
        /// </summary>
        public const string Cookie = "Cookie";

        /// <summary>
        /// Accept-Encoding HTTP Header.
        /// </summary>
        public const string AcceptEncoding = "Accept-Encoding";

        /// <summary>
        /// Content-Encoding HTTP Header.
        /// </summary>
        public const string ContentEncoding = "Content-Encoding";

        /// <summary>
        /// If-Modified-Since HTTP Header.
        /// </summary>
        public const string IfModifiedSince = "If-Modified-Since";

        /// <summary>
        /// Cache-Control HTTP Header.
        /// </summary>
        public const string CacheControl = "Cache-Control";
        
        /// <summary>
        /// The <c>Location</c> HTTP header.
        /// </summary>
        public const string Location = "Location";

        /// <summary>
        /// Pragma HTTP Header.
        /// </summary>
        public const string Pragma = "Pragma";

        /// <summary>
        /// Expires HTTP Header.
        /// </summary>
        public const string Expires = "Expires";

        /// <summary>
        /// Last-Modified HTTP Header.
        /// </summary>
        public const string LastModified = "Last-Modified";

        /// <summary>
        /// If-None-Match HTTP Header.
        /// </summary>
        public const string IfNotMatch = "If-None-Match";

        /// <summary>
        /// ETag HTTP Header.
        /// </summary>
        public const string ETag = "ETag";

        /// <summary>
        /// Accept-Ranges HTTP Header.
        /// </summary>
        public const string AcceptRanges = "Accept-Ranges";

        /// <summary>
        /// Range HTTP Header.
        /// </summary>
        public const string Range = "Range";

        /// <summary>
        /// Content-Range HTTP Header.
        /// </summary>
        public const string ContentRanges = "Content-Range";

        /// <summary>
        /// The header compression gzip.
        /// </summary>
        public const string CompressionGzip = "gzip";

        /// <summary>
        /// The web socket key.
        /// </summary>
        public const string WebSocketKey = "Sec-WebSocket-Key";

        /// <summary>
        /// The web socket version.
        /// </summary>
        public const string WebSocketVersion = "Sec-WebSocket-Version";

        /// <summary>
        /// The web socket protocol.
        /// </summary>
        public const string WebSocketProtocol = "Sec-WebSocket-Protocol";

        /// <summary>
        /// The web socket extensions.
        /// </summary>
        public const string WebSocketExtensions = "Sec-WebSocket-Extensions";

        /// <summary>
        /// The web socket accept.
        /// </summary>
        public const string WebSocketAccept = "Sec-WebSocket-Accept";
    }
}
