using System.Collections.Generic;
using System.Net;

namespace EmbedIO
{
    /// <summary>
    /// <para>Provides standard HTTP status descriptions.</para>
    /// <para>Data contained in this class comes from the following sources:</para>
    /// <list type="bullet">
    /// <item><description><see href="https://tools.ietf.org/html/rfc7231#section-6">RFC7231 Section 6</see> (HTTP/1.1 Semantics and Content)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc6585">RFC6585</see> (Additional HTTP Status Codes)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc2774#section-7">RFC2774 Section 7</see> (An HTTP Extension Framework)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc7540#section-9.1.2">RFC7540 Section 9.1.2</see> (HTTP/2)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc4918#section-11">RFC4918 Section 11</see> (WebDAV)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc5842#section-7">RFC5842 Section 7</see> (Binding Extensions to WebDAV)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc7538#section-3">RFC7538 Section 3</see> (HTTP Status Code 308)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc3229#section-10.4.1">RFC3229 Section 10.4.1</see> (Delta encoding in HTTP)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc8297#section-2">RFC8297 Section 2</see> (Early Hints)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc7725#section-3">RFC7725 Section 3</see> (HTTP-status-451)</description></item>
    /// <item><description><see href="https://tools.ietf.org/html/rfc2295#section-8.1">RFC2295 Section 8.1</see> (Transparent Content Negotiation)</description></item>
    /// </list>
    /// </summary>
    public static class HttpStatusDescription
    {
        private static readonly IReadOnlyDictionary<int, string> Dictionary = new Dictionary<int, string> {
            { 100, "Continue" },
            { 101, "Switching Protocols" },
            { 102, "Processing" },
            { 103, "Early Hints" },
            { 200, "OK" },
            { 201, "Created" },
            { 202, "Accepted" },
            { 203, "Non-Authoritative Information" },
            { 204, "No Content" },
            { 205, "Reset Content" },
            { 206, "Partial Content" },
            { 207, "Multi-Status" },
            { 208, "Already Reported" },
            { 226, "IM Used" },
            { 300, "Multiple Choices" },
            { 301, "Moved Permanently" },
            { 302, "Found" },
            { 303, "See Other" },
            { 304, "Not Modified" },
            { 305, "Use Proxy" },
            { 307, "Temporary Redirect" },
            { 308, "Permanent Redirect" },
            { 400, "Bad Request" },
            { 401, "Unauthorized" },
            { 402, "Payment Required" },
            { 403, "Forbidden" },
            { 404, "Not Found" },
            { 405, "Method Not Allowed" },
            { 406, "Not Acceptable" },
            { 407, "Proxy Authentication Required" },
            { 408, "Request Timeout" },
            { 409, "Conflict" },
            { 410, "Gone" },
            { 411, "Length Required" },
            { 412, "Precondition Failed" },
            { 413, "Request Entity Too Large" },
            { 414, "Request-Uri Too Long" },
            { 415, "Unsupported Media Type" },
            { 416, "Requested Range Not Satisfiable" },
            { 417, "Expectation Failed" },
            { 421, "Misdirected Request" },
            { 422, "Unprocessable Entity" },
            { 423, "Locked" },
            { 424, "Failed Dependency" },
            { 426, "Upgrade Required" },
            { 428, "Precondition Required" },
            { 429, "Too Many Requests" },
            { 431, "Request Header Fields Too Large" },
            { 451, "Unavailable For Legal Reasons" },
            { 500, "Internal Server Error" },
            { 501, "Not Implemented" },
            { 502, "Bad Gateway" },
            { 503, "Service Unavailable" },
            { 504, "Gateway Timeout" },
            { 505, "Http Version Not Supported" },
            { 506, "Variant Also Negotiates" },
            { 507, "Insufficient Storage" },
            { 508, "Loop Detected" },
            { 510, "Not Extended" },
            { 511, "Network Authentication Required" },
        };

        /// <summary>
        /// Attempts to get the standard status description for a <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="code">The HTTP status code for which the standard description
        /// is to be retrieved.</param>
        /// <param name="description">When this method returns, the standard HTTP status description
        /// for the specified <paramref name="code"/> if it was found, or <see langword="null"/>
        /// if it was not found. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified <paramref name="code"/> was found
        /// in the list of HTTP status codes for which the standard description is known;
        /// otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="TryGet(int,out string)"/>
        /// <seealso cref="Get(HttpStatusCode)"/>
        public static bool TryGet(HttpStatusCode code, out string description) => Dictionary.TryGetValue((int)code, out description);

        /// <summary>
        /// Attempts to get the standard status description for a HTTP status code
        /// specified as an <see langword="int"/>.
        /// </summary>
        /// <param name="code">The HTTP status code for which the standard description
        /// is to be retrieved.</param>
        /// <param name="description">When this method returns, the standard HTTP status description
        /// for the specified <paramref name="code"/> if it was found, or <see langword="null"/>
        /// if it was not found. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified <paramref name="code"/> was found
        /// in the list of HTTP status codes for which the standard description is known;
        /// otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="TryGet(HttpStatusCode,out string)"/>
        /// <seealso cref="Get(int)"/>
        public static bool TryGet(int code, out string description) => Dictionary.TryGetValue(code, out description);

        /// <summary>
        /// Returns the standard status description for a <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="code">The HTTP status code for which the standard description
        /// is to be retrieved.</param>
        /// <returns>The standard HTTP status description for the specified <paramref name="code"/>
        /// if it was found, or <see langword="null"/> if it was not found.</returns>
        public static string Get(HttpStatusCode code)
        {
            Dictionary.TryGetValue((int)code, out var description);
            return description;
        }

        /// <summary>
        /// Returns the standard status description for a HTTP status code
        /// specified as an <see langword="int"/>.
        /// </summary>
        /// <param name="code">The HTTP status code for which the standard description
        /// is to be retrieved.</param>
        /// <returns>The standard HTTP status description for the specified <paramref name="code"/>
        /// if it was found, or <see langword="null"/> if it was not found.</returns>
        public static string Get(int code)
        {
            Dictionary.TryGetValue(code, out var description);
            return description;
        }
    }
}