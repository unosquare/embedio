namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System.Net;
    using Swan.Formatters;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods to help your coding.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this IHttpContext context) => context.Response.NoCache();

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="response">The response.</param>
        public static void NoCache(this IHttpResponse response)
        {
            response.AddHeader(HttpHeaders.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            response.AddHeader(HttpHeaders.LastModified,
                DateTime.UtcNow.ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo));
            response.AddHeader(HttpHeaders.CacheControl, "no-store, no-cache, must-revalidate");
            response.AddHeader(HttpHeaders.Pragma, "no-cache");
        }
        
        /// <summary>
        /// Prepares a standard response without a body for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static void StandardResponseWithoutBody(this IHttpResponse @this, int statusCode)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.StatusCode = statusCode;
            @this.StatusDescription = statusDescription;
            @this.ContentType = string.Empty;
            @this.ContentLength64 = 0;
        }
        
        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="StandardHtmlResponseAsync(IHttpResponse,int,Func{StringBuilder,StringBuilder},CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(this IHttpResponse @this, int statusCode, CancellationToken cancellationToken)
            => StandardHtmlResponseAsync(@this, statusCode, null, cancellationToken);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="appendAdditionalHtml">A callback function that may append additional HTML code
        /// to the response. If not <see langword="null"/>, the callback is called immediately before
        /// closing the HTML <c>body</c> tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="StandardHtmlResponseAsync(IHttpResponse,int,CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(
            this IHttpResponse @this, 
            int statusCode, 
            Func<StringBuilder, StringBuilder> appendAdditionalHtml, 
            CancellationToken cancellationToken)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.StatusCode = statusCode;
            @this.StatusDescription = statusDescription;
            @this.ContentType = MimeTypes.HtmlType;
            var sb = new StringBuilder()
                .Append("<html><head><meta charset=\"UTF-8\"><title>")
                .Append(statusCode)
                .Append(" - ")
                .Append(statusDescription)
                .Append("</title></head><body><h1>")
                .Append(statusCode)
                .Append(" - ")
                .Append(statusDescription)
                .Append("</h1>");
            appendAdditionalHtml?.Invoke(sb);
            sb.Append("</body></html>");
            var buffer = Encoding.UTF8.GetBytes(sb.ToString());
            sb = null; // Free some memory if next GC is near
            @this.ContentLength64 = buffer.Length;
            return @this.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

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
        [Obsolete("This method will be replaced by SendStringAsync")]
        public static Task<bool> StringResponseAsync(
            this IHttpContext context,
            string content,
            string contentType = MimeTypes.JsonType,
            Encoding encoding = null,
            bool useGzip = true,
            CancellationToken cancellationToken = default) =>
            context.Response.StringResponseAsync(content, contentType, encoding, useGzip && context.AcceptGzip(content.Length), cancellationToken);

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        /// 
        [Obsolete("This method will be replaced by SendStringAsync")]
        public static async Task<bool> StringResponseAsync(
            this IHttpResponse response,
            string content,
            string contentType = MimeTypes.JsonType,
            Encoding encoding = null,
            bool useGzip = false,
            CancellationToken cancellationToken = default)
        {
            response.ContentType = contentType;

            using (var buffer = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(content)))
                return await BinaryResponseAsync(response, buffer, useGzip, cancellationToken).ConfigureAwait(false);
        }

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
            return context.BinaryResponseAsync(stream, useGzip, cancellationToken);
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
        [Obsolete("This method will be replaced by SendStreamAsync")]
        public static Task<bool> BinaryResponseAsync(
            this IHttpContext context,
            Stream buffer,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
            => BinaryResponseAsync(context.Response, buffer, useGzip && context.AcceptGzip(buffer.Length), cancellationToken);

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        [Obsolete("This method will be replaced by SendStreamAsync")]
        public static async Task<bool> BinaryResponseAsync(
            this IHttpResponse response,
            Stream buffer,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            if (useGzip)
            {
                buffer = await buffer.CompressAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                response.AddHeader(HttpHeaders.ContentEncoding, HttpHeaders.CompressionGzip);
            }

            response.ContentLength64 = buffer.Length;
            await response.WriteToOutputStream(buffer, 0, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Writes to output stream.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="lowerByteIndex">Index of the lower byte.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task representing the write operation to the stream.
        /// </returns>
        public static async Task WriteToOutputStream(
            this IHttpResponse response,
            Stream buffer,
            long lowerByteIndex = 0,
            CancellationToken cancellationToken = default)
        {
            buffer.Position = lowerByteIndex;
            await buffer.CopyToAsync(response.OutputStream, Modules.FileModuleBase.ChunkSize, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}