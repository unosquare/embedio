using System;
using System.Globalization;
using System.Net;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpRequest"/>.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// <para>Returns a string representing the remote IP address and port of an <see cref="IHttpRequest"/> interface.</para>
        /// <para>This method can be called even on a <see langword="null"/> interface, or one that has no
        /// remote end point, or no remote address; it will always return a non-<see langword="null"/>,
        /// non-empty string.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <returns>
        /// If <paramref name="this"/> is <see langword="null"/>, or its <see cref="IHttpRequest.RemoteEndPoint">RemoteEndPoint</see>
        /// is <see langword="null"/>, the string <c>"&lt;null&gt;</c>; otherwise, the remote end point's
        /// <see cref="IPEndPoint.Address">Address</see> (or the string <c>"&lt;???&gt;"</c> if it is <see langword="null"/>)
        /// followed by a colon and the <see cref="IPEndPoint.Port">Port</see> number.
        /// </returns>
        public static string SafeGetRemoteEndpointStr(this IHttpRequest @this)
        {
            var endPoint = @this?.RemoteEndPoint;
            if (endPoint == null)
                return "<null>";

            return $"{endPoint.Address?.ToString() ?? "<???>"}:{endPoint.Port.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// <para>Attempts to proactively negotiate a compression method for a response,
        /// based on a request's <c>Accept-Encoding</c> header (or lack of it).</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <param name="preferCompression"><see langword="true"/> if sending compressed data is preferred over
        /// sending non-compressed data; otherwise, <see langword="false"/>.</param>
        /// <param name="compressionMethod">When this method returns, the compression method to use for the response,
        /// if content negotiation is successful. This parameter is passed uninitialized.</param>
        /// <param name="prepareResponse">When this method returns, a callback that prepares data in a <see cref="IHttpResponse"/>
        /// according to the result of content negotiation. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if content negotiation is successful;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>If this method returns <see langword="true"/>, the <paramref name="prepareResponse"/> callback
        /// will set appropriate response headers to reflect the results of content negotiation.</para>
        /// <para>If this method returns <see langword="false"/>, the <paramref name="prepareResponse"/> callback
        /// will throw a <see cref="HttpNotAcceptableException"/> to send a <c>406 Not Acceptable</c> response
        /// with the <c>Vary</c> header set to <c>Accept-Encoding</c>,
        /// so that the client may know the reason why the request has been rejected.</para>
        /// <para>If <paramref name="this"/> has no<c>Accept-Encoding</c> header, this method
        /// always returns <see langword="true"/> and sets <paramref name="compressionMethod"/>
        /// to <see cref="CompressionMethod.None"/>.</para>
        /// </remarks>
        /// <seealso cref="HttpNotAcceptableException(string)"/>
        public static bool TryNegotiateContentEncoding(
            this IHttpRequest @this,
            bool preferCompression,
            out CompressionMethod compressionMethod,
            out Action<IHttpResponse> prepareResponse)
        {
            compressionMethod = CompressionMethod.None;
            var acceptedEncodings = new QValueList(true, @this.Headers.GetValues(HttpHeaderNames.AcceptEncoding));
            if (acceptedEncodings.QValues.Count < 1)
            {
                prepareResponse = r => r.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethodNames.None);
                return true;
            }

            var acceptableMethods = preferCompression
                ? new[] { CompressionMethod.Gzip, CompressionMethod.Deflate, CompressionMethod.None }
                : new[] { CompressionMethod.None, CompressionMethod.Gzip, CompressionMethod.Deflate };
            var acceptableMethodNames = preferCompression
                ? new[] { CompressionMethodNames.Gzip, CompressionMethodNames.Deflate, CompressionMethodNames.None }
                : new[] { CompressionMethodNames.None, CompressionMethodNames.Gzip, CompressionMethodNames.Deflate };

            var acceptableMethodIndex = acceptedEncodings.FindPreferredIndex(acceptableMethodNames);
            if (acceptableMethodIndex < 0)
            {
                prepareResponse = r => throw HttpException.NotAcceptable(HttpHeaderNames.AcceptEncoding);
                return false;
            }

            compressionMethod = acceptableMethods[acceptableMethodIndex];
            var acceptedMethodName = acceptableMethodNames[acceptableMethodIndex];
            prepareResponse = r => {
                r.Headers.Add(HttpHeaderNames.Vary, HttpHeaderNames.AcceptEncoding);
                r.Headers.Set(HttpHeaderNames.ContentEncoding, acceptedMethodName);
            };
            return true;
        }
    }
}