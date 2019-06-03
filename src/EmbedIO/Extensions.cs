using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Files;
using EmbedIO.Internal;
using Unosquare.Swan.Formatters;

namespace EmbedIO
{
    /// <summary>
    /// Extension methods to help your coding.
    /// </summary>
    public static partial class Extensions
    {
        #region HTTP Request Helpers

        /// <summary>
        /// Parses the JSON as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T">The type of specified object type.</typeparam>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A task with the JSON as a given type from the request body.
        /// </returns>
        public static async Task<T> ParseJsonAsync<T>(this IHttpContext context)
            where T : class
        {
            var requestBody = await context.Request.GetBodyAsStringAsync().ConfigureAwait(false);
            return requestBody == null ? null : Json.Deserialize<T>(requestBody);
        }

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJson<TIn, TOut>(
            this IHttpContext context,
            Func<TIn, CancellationToken, Task<TOut>> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await context.ParseJsonAsync<TIn>().ConfigureAwait(false);
            var responseJson = await transformFunc(requestJson, cancellationToken).ConfigureAwait(false);
            return await context.JsonResponseAsync(responseJson, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJson<TIn, TOut>(
            this IHttpContext context,
            Func<TIn, TOut> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await context.ParseJsonAsync<TIn>().ConfigureAwait(false);
            var responseJson = transformFunc(requestJson);
            return await context.JsonResponseAsync(responseJson, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check if the Http Request can be gzipped (ignore audio and video content type).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="length">The length.</param>
        /// <returns><c>true</c> if a request can be gzipped; otherwise, <c>false</c>.</returns>
        public static bool AcceptGzip(this IHttpContext context, long length)
        {
            var acceptEncoding = context.Request.Headers[HttpHeaderNames.AcceptEncoding];
            if (acceptEncoding == null)
                return false;

            var contentType = context.Response.ContentType;
            if (contentType != null)
            {
                if (contentType.StartsWith("audio/", StringComparison.Ordinal) || contentType.StartsWith("video/", StringComparison.Ordinal))
                    return false;
            }

            return acceptEncoding.Contains(CompressionMethods.Gzip)
                 && length <= EmbedIOConstants.MaxGzipLength;
        }

        #endregion

        #region Data Parsing Methods

        /// <summary>
        /// Returns a dictionary of KVPs from Request data.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns>A collection that represents KVPs from request data.</returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
            => FormDataParser.ParseAsDictionary(requestBody);

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context">The context to request body as string.</param>
        /// <returns>A task with a collection that represents KVPs from request data.</returns>
        public static async Task<Dictionary<string, object>> RequestFormDataDictionaryAsync(this IHttpContext context)
            => RequestFormDataDictionary(await context.Request.GetBodyAsStringAsync().ConfigureAwait(false));

        #endregion

        #region Hashing and Compression Methods


        #endregion
    }
}