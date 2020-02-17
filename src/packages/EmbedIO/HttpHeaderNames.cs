namespace EmbedIO
{
    /// <summary>
    /// Exposes known HTTP header names.
    /// </summary>
    /// <remarks>
    /// <para>The constants in this class have been extracted from a list of known HTTP header names.
    /// The presence of a header name in this class is not a guarantee that EmbedIO supports,
    /// or even recognizes, it. Refer to the documentation for each module for information about supported
    /// headers.</para>
    /// </remarks>
    public static class HttpHeaderNames
    {
        // The .NET Core sources were taken as reference for this list of constants.
        // See https://github.com/dotnet/corefx/blob/master/src/Common/src/System/Net/HttpKnownHeaderNames.cs
        // However, not all constants come from there, so be careful not to copy-paste indiscriminately.

        /// <summary>
        /// The <c>Accept</c> HTTP header.
        /// </summary>
        public const string Accept = "Accept";

        /// <summary>
        /// The <c>Accept-Charset</c> HTTP header.
        /// </summary>
        public const string AcceptCharset = "Accept-Charset";

        /// <summary>
        /// The <c>Accept-Encoding</c> HTTP header.
        /// </summary>
        public const string AcceptEncoding = "Accept-Encoding";

        /// <summary>
        /// The <c>Accept-Language</c> HTTP header.
        /// </summary>
        public const string AcceptLanguage = "Accept-Language";

        /// <summary>
        /// The <c>Accept-Patch</c> HTTP header.
        /// </summary>
        public const string AcceptPatch = "Accept-Patch";

        /// <summary>
        /// The <c>Accept-Ranges</c> HTTP header.
        /// </summary>
        public const string AcceptRanges = "Accept-Ranges";

        /// <summary>
        /// The <c>Access-Control-Allow-Credentials</c> HTTP header.
        /// </summary>
        public const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";

        /// <summary>
        /// The <c>Access-Control-Allow-Headers</c> HTTP header.
        /// </summary>
        public const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";

        /// <summary>
        /// The <c>Access-Control-Allow-Methods</c> HTTP header.
        /// </summary>
        public const string AccessControlAllowMethods = "Access-Control-Allow-Methods";

        /// <summary>
        /// The <c>Access-Control-Allow-Origin</c> HTTP header.
        /// </summary>
        public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

        /// <summary>
        /// The <c>Access-Control-Expose-Headers</c> HTTP header.
        /// </summary>
        public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";

        /// <summary>
        /// The <c>Access-Control-Max-Age</c> HTTP header.
        /// </summary>
        public const string AccessControlMaxAge = "Access-Control-Max-Age";

        /// <summary>
        /// The <c>Access-Control-Request-Headers</c> HTTP header.
        /// </summary>
        public const string AccessControlRequestHeaders = "Access-Control-Request-Headers";

        /// <summary>
        /// The <c>Access-Control-Request-Method</c> HTTP header.
        /// </summary>
        public const string AccessControlRequestMethod = "Access-Control-Request-Method";

        /// <summary>
        /// The <c>Age</c> HTTP header.
        /// </summary>
        public const string Age = "Age";

        /// <summary>
        /// The <c>Allow</c> HTTP header.
        /// </summary>
        public const string Allow = "Allow";

        /// <summary>
        /// The <c>Alt-Svc</c> HTTP header.
        /// </summary>
        public const string AltSvc = "Alt-Svc";

        /// <summary>
        /// The <c>Authorization</c> HTTP header.
        /// </summary>
        public const string Authorization = "Authorization";

        /// <summary>
        /// The <c>Cache-Control</c> HTTP header.
        /// </summary>
        public const string CacheControl = "Cache-Control";

        /// <summary>
        /// The <c>Connection</c> HTTP header.
        /// </summary>
        public const string Connection = "Connection";

        /// <summary>
        /// The <c>Content-Disposition</c> HTTP header.
        /// </summary>
        public const string ContentDisposition = "Content-Disposition";

        /// <summary>
        /// The <c>Content-Encoding</c> HTTP header.
        /// </summary>
        public const string ContentEncoding = "Content-Encoding";

        /// <summary>
        /// The <c>Content-Language</c> HTTP header.
        /// </summary>
        public const string ContentLanguage = "Content-Language";

        /// <summary>
        /// The <c>Content-Length</c> HTTP header.
        /// </summary>
        public const string ContentLength = "Content-Length";

        /// <summary>
        /// The <c>Content-Location</c> HTTP header.
        /// </summary>
        public const string ContentLocation = "Content-Location";

        /// <summary>
        /// The <c>Content-MD5</c> HTTP header.
        /// </summary>
        public const string ContentMD5 = "Content-MD5";

        /// <summary>
        /// The <c>Content-Range</c> HTTP header.
        /// </summary>
        public const string ContentRange = "Content-Range";

        /// <summary>
        /// The <c>Content-Security-Policy</c> HTTP header.
        /// </summary>
        public const string ContentSecurityPolicy = "Content-Security-Policy";

        /// <summary>
        /// The <c>Content-Type</c> HTTP header.
        /// </summary>
        public const string ContentType = "Content-Type";

        /// <summary>
        /// The <c>Cookie</c> HTTP header.
        /// </summary>
        public const string Cookie = "Cookie";

        /// <summary>
        /// The <c>Cookie2</c> HTTP header.
        /// </summary>
        public const string Cookie2 = "Cookie2";

        /// <summary>
        /// The <c>Date</c> HTTP header.
        /// </summary>
        public const string Date = "Date";

        /// <summary>
        /// The <c>ETag</c> HTTP header.
        /// </summary>
        public const string ETag = "ETag";

        /// <summary>
        /// The <c>Expect</c> HTTP header.
        /// </summary>
        public const string Expect = "Expect";

        /// <summary>
        /// The <c>Expires</c> HTTP header.
        /// </summary>
        public const string Expires = "Expires";

        /// <summary>
        /// The <c>From</c> HTTP header.
        /// </summary>
        public const string From = "From";

        /// <summary>
        /// The <c>Host</c> HTTP header.
        /// </summary>
        public const string Host = "Host";

        /// <summary>
        /// The <c>If-Match</c> HTTP header.
        /// </summary>
        public const string IfMatch = "If-Match";

        /// <summary>
        /// The <c>If-Modified-Since</c> HTTP header.
        /// </summary>
        public const string IfModifiedSince = "If-Modified-Since";

        /// <summary>
        /// The <c>If-None-Match</c> HTTP header.
        /// </summary>
        public const string IfNoneMatch = "If-None-Match";

        /// <summary>
        /// The <c>If-Range</c> HTTP header.
        /// </summary>
        public const string IfRange = "If-Range";

        /// <summary>
        /// The <c>If-Unmodified-Since</c> HTTP header.
        /// </summary>
        public const string IfUnmodifiedSince = "If-Unmodified-Since";

        /// <summary>
        /// The <c>Keep-Alive</c> HTTP header.
        /// </summary>
        public const string KeepAlive = "Keep-Alive";

        /// <summary>
        /// The <c>Last-Modified</c> HTTP header.
        /// </summary>
        public const string LastModified = "Last-Modified";

        /// <summary>
        /// The <c>Link</c> HTTP header.
        /// </summary>
        public const string Link = "Link";

        /// <summary>
        /// The <c>Location</c> HTTP header.
        /// </summary>
        public const string Location = "Location";

        /// <summary>
        /// The <c>Max-Forwards</c> HTTP header.
        /// </summary>
        public const string MaxForwards = "Max-Forwards";

        /// <summary>
        /// The <c>Origin</c> HTTP header.
        /// </summary>
        public const string Origin = "Origin";

        /// <summary>
        /// The <c>P3P</c> HTTP header.
        /// </summary>
        public const string P3P = "P3P";

        /// <summary>
        /// The <c>Pragma</c> HTTP header.
        /// </summary>
        public const string Pragma = "Pragma";

        /// <summary>
        /// The <c>Proxy-Authenticate</c> HTTP header.
        /// </summary>
        public const string ProxyAuthenticate = "Proxy-Authenticate";

        /// <summary>
        /// The <c>Proxy-Authorization</c> HTTP header.
        /// </summary>
        public const string ProxyAuthorization = "Proxy-Authorization";

        /// <summary>
        /// The <c>Proxy-Connection</c> HTTP header.
        /// </summary>
        public const string ProxyConnection = "Proxy-Connection";

        /// <summary>
        /// The <c>Public-Key-Pins</c> HTTP header.
        /// </summary>
        public const string PublicKeyPins = "Public-Key-Pins";

        /// <summary>
        /// The <c>Range</c> HTTP header.
        /// </summary>
        public const string Range = "Range";

        /// <summary>
        /// The <c>Referer</c> HTTP header.
        /// </summary>
        /// <remarks>
        /// <para>The incorrect spelling ("Referer" instead of "Referrer") is intentional
        /// and has historical reasons.</para>
        /// <para>See the "Etymology" section of <a href="https://en.wikipedia.org/wiki/HTTP_referer">the Wikipedia article</a>
        /// on this header for more information.</para>
        /// </remarks>
        public const string Referer = "Referer";

        /// <summary>
        /// The <c>Retry-After</c> HTTP header.
        /// </summary>
        public const string RetryAfter = "Retry-After";

        /// <summary>
        /// The <c>Sec-WebSocket-Accept</c> HTTP header.
        /// </summary>
        public const string SecWebSocketAccept = "Sec-WebSocket-Accept";

        /// <summary>
        /// The <c>Sec-WebSocket-Extensions</c> HTTP header.
        /// </summary>
        public const string SecWebSocketExtensions = "Sec-WebSocket-Extensions";

        /// <summary>
        /// The <c>Sec-WebSocket-Key</c> HTTP header.
        /// </summary>
        public const string SecWebSocketKey = "Sec-WebSocket-Key";

        /// <summary>
        /// The <c>Sec-WebSocket-Protocol</c> HTTP header.
        /// </summary>
        public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";

        /// <summary>
        /// The <c>Sec-WebSocket-Version</c> HTTP header.
        /// </summary>
        public const string SecWebSocketVersion = "Sec-WebSocket-Version";

        /// <summary>
        /// The <c>Server</c> HTTP header.
        /// </summary>
        public const string Server = "Server";

        /// <summary>
        /// The <c>Set-Cookie</c> HTTP header.
        /// </summary>
        public const string SetCookie = "Set-Cookie";

        /// <summary>
        /// The <c>Set-Cookie2</c> HTTP header.
        /// </summary>
        public const string SetCookie2 = "Set-Cookie2";

        /// <summary>
        /// The <c>Strict-Transport-Security</c> HTTP header.
        /// </summary>
        public const string StrictTransportSecurity = "Strict-Transport-Security";

        /// <summary>
        /// The <c>TE</c> HTTP header.
        /// </summary>
        public const string TE = "TE";

        /// <summary>
        /// The <c>TSV</c> HTTP header.
        /// </summary>
        public const string TSV = "TSV";

        /// <summary>
        /// The <c>Trailer</c> HTTP header.
        /// </summary>
        public const string Trailer = "Trailer";

        /// <summary>
        /// The <c>Transfer-Encoding</c> HTTP header.
        /// </summary>
        public const string TransferEncoding = "Transfer-Encoding";

        /// <summary>
        /// The <c>Upgrade</c> HTTP header.
        /// </summary>
        public const string Upgrade = "Upgrade";

        /// <summary>
        /// The <c>Upgrade-Insecure-Requests</c> HTTP header.
        /// </summary>
        public const string UpgradeInsecureRequests = "Upgrade-Insecure-Requests";

        /// <summary>
        /// The <c>User-Agent</c> HTTP header.
        /// </summary>
        public const string UserAgent = "User-Agent";

        /// <summary>
        /// The <c>Vary</c> HTTP header.
        /// </summary>
        public const string Vary = "Vary";

        /// <summary>
        /// The <c>Via</c> HTTP header.
        /// </summary>
        public const string Via = "Via";

        /// <summary>
        /// The <c>WWW-Authenticate</c> HTTP header.
        /// </summary>
        public const string WWWAuthenticate = "WWW-Authenticate";

        /// <summary>
        /// The <c>Warning</c> HTTP header.
        /// </summary>
        public const string Warning = "Warning";

        /// <summary>
        /// The <c>X-AspNet-Version</c> HTTP header.
        /// </summary>
        public const string XAspNetVersion = "X-AspNet-Version";

        /// <summary>
        /// The <c>X-Content-Duration</c> HTTP header.
        /// </summary>
        public const string XContentDuration = "X-Content-Duration";

        /// <summary>
        /// The <c>X-Content-Type-Options</c> HTTP header.
        /// </summary>
        public const string XContentTypeOptions = "X-Content-Type-Options";

        /// <summary>
        /// The <c>X-Frame-Options</c> HTTP header.
        /// </summary>
        public const string XFrameOptions = "X-Frame-Options";

        /// <summary>
        /// The <c>X-MSEdge-Ref</c> HTTP header.
        /// </summary>
        public const string XMSEdgeRef = "X-MSEdge-Ref";

        /// <summary>
        /// The <c>X-Powered-By</c> HTTP header.
        /// </summary>
        public const string XPoweredBy = "X-Powered-By";

        /// <summary>
        /// The <c>X-Request-ID</c> HTTP header.
        /// </summary>
        public const string XRequestID = "X-Request-ID";

        /// <summary>
        /// The <c>X-UA-Compatible</c> HTTP header.
        /// </summary>
        public const string XUACompatible = "X-UA-Compatible";
    }
}