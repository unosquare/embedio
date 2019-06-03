using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan.Formatters;

namespace EmbedIO
{
    /// <summary>
    /// Extension methods to help your coding.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Outputs async a Json Response given a data object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <c>true</c> value if the response output was set.
        /// </returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            object data,
            bool useGzip,
            CancellationToken cancellationToken = default)
            => context.JsonResponseAsync(Json.Serialize(data), useGzip, cancellationToken);
        
        /// <summary>
        /// Outputs async a Json Response given a data object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <c>true</c> value if the response output was set.
        /// </returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            object data,
            CancellationToken cancellationToken = default)
            => context.JsonResponseAsync(Json.Serialize(data), cancellationToken);

        /// <summary>
        /// Outputs async a JSON Response given a JSON string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            string json,
            bool useGzip,
            CancellationToken cancellationToken = default)
            => context.StringResponseAsync(json, cancellationToken: cancellationToken, useGzip: useGzip);

        /// <summary>
        /// Outputs async a JSON Response given a JSON string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            string json,
            CancellationToken cancellationToken = default)
            => context.StringResponseAsync(json, cancellationToken: cancellationToken);

        /// <summary>
        /// Outputs a HTML Response given a HTML content.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="htmlContent">Content of the HTML.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> HtmlResponseAsync(
            this IHttpContext context,
            string htmlContent,
            HttpStatusCode statusCode = HttpStatusCode.OK,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.StringResponseAsync(htmlContent, MimeTypes.HtmlType, null, useGzip, cancellationToken);
        }

        /// <summary>
        /// Outputs a JSON Response given an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> JsonExceptionResponseAsync(
            this IHttpContext context,
            Exception ex,
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.JsonResponseAsync(ex, useGzip, cancellationToken);
        }

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> StringResponseAsync(
            this IHttpContext context,
            string content,
            string contentType = MimeTypes.JsonType,
            Encoding encoding = null,
            bool useGzip = true,
            CancellationToken cancellationToken = default) =>
            context.Response.SendStringAsync(content, contentType, encoding, useGzip && context.AcceptGzip(content.Length), cancellationToken);

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> FileResponseAsync(
            this IHttpContext context,
            FileInfo file,
            string contentType = null,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            context.Response.ContentType = contentType ?? MimeTypes.HtmlType;

            var stream = file.OpenRead();
            return context.BinaryResponseAsync(stream, useGzip, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> BinaryResponseAsync(
            this IHttpContext context,
            Stream buffer,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
            => context.Response.SendStreamAsync(buffer, useGzip && context.AcceptGzip(buffer.Length), cancellationToken);
    }
}