using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
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
            return endPoint == null
                ? "<null>"
                : $"{endPoint.Address?.ToString() ?? "<???>"}:{endPoint.Port.ToString(CultureInfo.InvariantCulture)}";
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
        /// <param name="prepareResponse">When this method returns, a callback that prepares data in an <see cref="IHttpResponse"/>
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
            var acceptedEncodings = new QValueList(true, @this.Headers.GetValues(HttpHeaderNames.AcceptEncoding));
            if (!acceptedEncodings.TryNegotiateContentEncoding(preferCompression, out compressionMethod, out var compressionMethodName))
            {
                prepareResponse = r => throw HttpException.NotAcceptable(HttpHeaderNames.AcceptEncoding);
                return false;
            }

            prepareResponse = r => {
                r.Headers.Add(HttpHeaderNames.Vary, HttpHeaderNames.AcceptEncoding);
                r.Headers.Set(HttpHeaderNames.ContentEncoding, compressionMethodName);
            };
            return true;
        }

        /// <summary>
        /// <para>Checks whether an <c>If-None-Match</c> header exists in a request
        /// and, if so, whether it contains a given entity tag.</para>
        /// <para>See <see href="https://tools.ietf.org/html/rfc7232#section-3.2">RFC7232, Section 3.2</see>
        /// for a normative reference; however, see the Remarks section for more information
        /// about the RFC compliance of this method.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <param name="entityTag">The entity tag.</param>
        /// <param name="headerExists">When this method returns, a value that indicates whether an
        /// <c>If-None-Match</c> header is present in <paramref name="this"/>, regardless of the method's
        /// return value. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if an <c>If-None-Match</c> header is present in
        /// <paramref name="this"/> and one of the entity tags listed in it is equal to <paramref name="entityTag"/>;
        /// <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// <para><see href="https://tools.ietf.org/html/rfc7232#section-3.2">RFC7232, Section 3.2</see>
        /// states that a weak comparison function (as defined in
        /// <see href="https://tools.ietf.org/html/rfc7232#section-2.3.2">RFC7232, Section 2.3.2</see>)
        /// must be used for <c>If-None-Match</c>. That would mean parsing every entity tag, at least minimally,
        /// to determine whether it is a "weak" or "strong" tag. Since EmbedIO currently generates only
        /// "strong" tags, this method uses the default string comparer instead.</para>
        /// <para>The behavior of this method is thus not, strictly speaking, RFC7232-compliant;
        /// it works, though, with entity tags generated by EmbedIO.</para>
        /// </remarks>
        public static bool CheckIfNoneMatch(this IHttpRequest @this, string entityTag, out bool headerExists)
        {
            var values = @this.Headers.GetValues(HttpHeaderNames.IfNoneMatch);
            if (values == null)
            {
                headerExists = false;
                return false;
            }

            headerExists = true;
            return values.Select(t => t.Trim()).Contains(entityTag);
        }

        // Check whether the If-Modified-Since request header exists
        // and specifies a date and time more recent than or equal to
        // the date and time of last modification of the requested resource.
        // RFC7232, Section 3.3

        /// <summary>
        /// <para>Checks whether an <c>If-Modified-Since</c> header exists in a request
        /// and, if so, whether its value is a date and time more recent or equal to
        /// a given <see cref="DateTime"/>.</para>
        /// <para>See <see href="https://tools.ietf.org/html/rfc7232#section-3.3">RFC7232, Section 3.3</see>
        /// for a normative reference.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <param name="lastModifiedUtc">A date and time value, in Coordinated Universal Time,
        /// expressing the last time a resource was modified.</param>
        /// <param name="headerExists">When this method returns, a value that indicates whether an
        /// <c>If-Modified-Since</c> header is present in <paramref name="this"/>, regardless of the method's
        /// return value. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if an <c>If-Modified-Since</c> header is present in
        /// <paramref name="this"/> and its value is a date and time more recent or equal to <paramref name="lastModifiedUtc"/>;
        /// <see langword="false"/> otherwise.</returns>
        public static bool CheckIfModifiedSince(this IHttpRequest @this, DateTime lastModifiedUtc, out bool headerExists)
        {
            var value = @this.Headers.Get(HttpHeaderNames.IfModifiedSince);
            if (value == null)
            {
                headerExists = false;
                return false;
            }

            headerExists = true;
            return HttpDate.TryParse(value, out var dateTime)
                && dateTime.UtcDateTime >= lastModifiedUtc;
        }

        // Checks the Range request header to tell whether to send
        // a "206 Partial Content" response.

        /// <summary>
        /// <para>Checks whether a <c>Range</c> header exists in a request
        /// and, if so, determines whether it is possible to send a <c>206 Partial Content</c> response.</para>
        /// <para>See <see href="https://tools.ietf.org/html/rfc7233">RFC7233</see>
        /// for a normative reference; however, see the Remarks section for more information
        /// about the RFC compliance of this method.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <param name="contentLength">The total length, in bytes, of the response entity, i.e.
        /// what would be sent in a <c>200 OK</c> response.</param>
        /// <param name="entityTag">An entity tag representing the response entity. This value is checked against
        /// the <c>If-Range</c> header, if it is present.</param>
        /// <param name="lastModifiedUtc">The date and time value, in Coordinated Universal Time,
        /// expressing the last modification time of the resource entity. This value is checked against
        /// the <c>If-Range</c> header, if it is present.</param>
        /// <param name="start">When this method returns <see langword="true"/>, the start of the requested byte range.
        /// This parameter is passed uninitialized.</param>
        /// <param name="upperBound">
        /// <para>When this method returns <see langword="true"/>, the upper bound of the requested byte range.
        /// This parameter is passed uninitialized.</para>
        /// <para>Note that the upper bound of a range is NOT the sum of the range's start and length;
        /// for example, a range expressed as <c>bytes=0-99</c> has a start of 0, an upper bound of 99,
        /// and a length of 100 bytes.</para>
        /// </param>
        /// <returns>
        /// <para>This method returns <see langword="true"/> if the following conditions are satisfied:</para>
        /// <list type="bullet">
        /// <item><description>>the request's HTTP method is <c>GET</c>;</description></item>
        /// <item><description>>a <c>Range</c> header is present in the request;</description></item>
        /// <item><description>>either no <c>If-Range</c> header is present in the request, or it
        /// specifies an entity tag equal to <paramref name="entityTag"/>, or a UTC date and time
        /// equal to <paramref name="lastModifiedUtc"/>;</description></item>
        /// <item><description>>the <c>Range</c> header specifies exactly one range;</description></item>
        /// <item><description>>the specified range is entirely contained in the range from 0 to <paramref name="contentLength"/> - 1.</description></item>
        /// </list>
        /// <para>If the last condition is not satisfied, i.e. the specified range start and/or upper bound
        /// are out of the range from 0 to <paramref name="contentLength"/> - 1, this method does not return;
        /// it throws a <see cref="HttpRangeNotSatisfiableException"/> instead.</para>
        /// <para>If any of the other conditions are not satisfied, this method returns <see langword="false"/>.</para>
        /// </returns>
        /// <remarks>
        /// <para>According to <see href="https://tools.ietf.org/html/rfc7233#section-3.1">RFC7233, Section 3.1</see>,
        /// there are several conditions under which a server may ignore or reject a range request; therefore,
        /// clients are (or should be) prepared to receive a <c>200 OK</c> response with the whole response
        /// entity instead of the requested range(s). For this reason, until the generation of
        /// <c>multipart/byteranges</c> responses is implemented in EmbedIO, this method will ignore
        /// range requests specifying more than one range, even if this behavior is not, strictly speaking,
        /// RFC7233-compliant.</para>
        /// <para>To make clients aware that range requests are accepted for a resource, every <c>200 OK</c>
        /// (or <c>304 Not Modified</c>) response for the same resource should include an <c>Accept-Ranges</c>
        /// header with the string <c>bytes</c> as value.</para>
        /// </remarks>
        public static bool IsRangeRequest(this IHttpRequest @this, long contentLength, string entityTag, DateTime lastModifiedUtc, out long start, out long upperBound)
        {
            start = 0;
            upperBound = contentLength - 1;

            // RFC7233, Section 3.1:
            // "A server MUST ignore a Range header field received with a request method other than GET."
            if (@this.HttpVerb != HttpVerbs.Get)
                return false;

            // No Range header, no partial content.
            var rangeHeader = @this.Headers.Get(HttpHeaderNames.Range);
            if (rangeHeader == null)
                return false;

            // Ignore the Range header if there is no If-Range header
            // or if the If-Range header specifies a non-matching validator.
            // RFC7233, Section 3.2: "If the validator given in the If-Range header field matches the
            //                       current validator for the selected representation of the target
            //                       resource, then the server SHOULD process the Range header field as
            //                       requested.If the validator does not match, the server MUST ignore
            //                       the Range header field.Note that this comparison by exact match,
            //                       including when the validator is an HTTP-date, differs from the
            //                       "earlier than or equal to" comparison used when evaluating an
            //                       If-Unmodified-Since conditional."
            var ifRange = @this.Headers.Get(HttpHeaderNames.IfRange)?.Trim();
            if (ifRange != null && ifRange != entityTag)
            {
                if (!HttpDate.TryParse(ifRange, out var rangeDate))
                    return false;

                if (rangeDate.UtcDateTime != lastModifiedUtc)
                    return false;
            }

            // Ignore the Range request header if it cannot be parsed successfully.
            if (!RangeHeaderValue.TryParse(rangeHeader, out var range))
                return false;

            // EmbedIO does not support multipart/byteranges responses (yet),
            // thus ignore range requests that specify one range.
            if (range.Ranges.Count != 1)
                return false;

            var firstRange = range.Ranges.First();
            start = firstRange.From ?? 0L;
            upperBound = firstRange.To ?? contentLength - 1;
            if (start >= contentLength || upperBound < start || upperBound >= contentLength)
                throw HttpException.RangeNotSatisfiable(contentLength);

            return true;
        }
    }
}