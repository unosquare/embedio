namespace Unosquare.Labs.EmbedIO
{
    using Constants;
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
            response.AddHeader(Headers.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            response.AddHeader(Headers.LastModified,
                DateTime.UtcNow.ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo));
            response.AddHeader(Headers.CacheControl, "no-store, no-cache, must-revalidate");
            response.AddHeader(Headers.Pragma, "no-cache");
        }

        /// <summary>
        /// Sets a response static code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        /// <returns><b>true</b> if the headers were set, otherwise <b>false</b>.</returns>
        public static bool Redirect(this IHttpContext context, string location, bool useAbsoluteUrl = true)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetComponents(UriComponents.Scheme | UriComponents.StrongAuthority,
                    UriFormat.Unescaped);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);

            return true;
        }

        /// <summary>
        /// Outputs a Json Response given a data object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns>A <c>true</c> value of type ref=JsonResponseAsync".</returns>
        [Obsolete("Please use the async method.")]
        public static bool JsonResponse(this IHttpContext context, object data)
            => context.JsonResponseAsync(data).GetAwaiter().GetResult();

        /// <summary>
        /// Outputs async a Json Response given a data object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>
        /// A <c>true</c> value of type ref=JsonResponseAsync".
        /// </returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            object data,
            CancellationToken cancellationToken = default,
            bool useGzip = true)
            => context.JsonResponseAsync(Json.Serialize(data), cancellationToken, useGzip);

        /// <summary>
        /// Outputs a Json Response given a Json string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <returns> A <c>true</c> value of type ref=JsonResponseAsync".</returns>
        [Obsolete("Please use the async method.")]
        public static bool JsonResponse(this IHttpContext context, string json)
            => context.JsonResponseAsync(json).GetAwaiter().GetResult();

        /// <summary>
        /// Outputs async a JSON Response given a JSON string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>A task for writing the output stream.</returns>
        public static Task<bool> JsonResponseAsync(
            this IHttpContext context,
            string json,
            CancellationToken cancellationToken = default,
            bool useGzip = true)
            => context.StringResponseAsync(json, cancellationToken: cancellationToken, useGzip: useGzip);

        /// <summary>
        /// Outputs a HTML Response given a HTML content.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="htmlContent">Content of the HTML.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>A task for writing the output stream.</returns>
        public static Task<bool> HtmlResponseAsync(
            this IHttpContext context,
            string htmlContent,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK,
            CancellationToken cancellationToken = default,
            bool useGzip = true)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.StringResponseAsync(htmlContent, Responses.HtmlContentType, cancellationToken, useGzip: useGzip);
        }

        /// <summary>
        /// Outputs async a JSON Response given an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>A <c>true</c> value when the exception is written to the HTTP Response.</returns>
        [Obsolete("Please use the async method.")]
        public static bool JsonExceptionResponse(
            this IHttpContext context,
            Exception ex,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.JsonResponse(ex);
        }

        /// <summary>
        /// Outputs a JSON Response given an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>A task for writing the output stream.</returns>
        public static Task<bool> JsonExceptionResponseAsync(
            this IHttpContext context,
            Exception ex,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError,
            bool useGzip = true)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.JsonResponseAsync(ex, useGzip: useGzip);
        }

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> StringResponseAsync(
            this IHttpContext context,
            string content,
            string contentType = "application/json",
            CancellationToken cancellationToken = default,
            Encoding encoding = null,
            bool useGzip = true)
        {
            return context.Response.StringResponseAsync(content, contentType, cancellationToken, encoding, useGzip && context.AcceptGzip(content.Length));
        }

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> StringResponseAsync(
            this IHttpResponse response,
            string content,
            string contentType = "application/json",
            CancellationToken cancellationToken = default,
            Encoding encoding = null,
            bool useGzip = false)
        {
            response.ContentType = contentType;

            using (var buffer = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(content)))
                return BinaryResponseAsync(response, buffer, cancellationToken, useGzip);
        }

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">The file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="ct">The ct.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> FileResponseAsync(
            this IHttpContext context,
            FileInfo file,
            string contentType = null,
            CancellationToken ct = default,
            bool useGzip = true)
        {
            context.Response.ContentType = contentType ?? Responses.HtmlContentType;

            using (var stream = file.OpenRead())
                return context.BinaryResponseAsync(stream, ct, useGzip);
        }

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="ct">The ct.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static Task<bool> BinaryResponseAsync(
            this IHttpContext context,
            Stream buffer,
            CancellationToken ct = default,
            bool useGzip = true)
            => BinaryResponseAsync(context.Response, buffer, ct, useGzip && context.AcceptGzip(buffer.Length));

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="ct">The ct.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <returns>A task for writing the output stream.</returns>
        public static async Task<bool> BinaryResponseAsync(
            this IHttpResponse response,
            Stream buffer,
            CancellationToken ct = default,
            bool useGzip = true)
        {
            if (useGzip)
            {
                buffer = await buffer.CompressAsync(cancellationToken: ct).ConfigureAwait(false);
                response.AddHeader(Headers.ContentEncoding, Headers.CompressionGzip);
            }

            response.ContentLength64 = buffer.Length;
            await response.WriteToOutputStream(buffer, 0, ct).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Writes to output stream.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="lowerByteIndex">Index of the lower byte.</param>
        /// <param name="ct">The ct.</param>
        /// <returns>A task representing the write operation to the stream.</returns>
        public static async Task WriteToOutputStream(
            this IHttpResponse response,
            Stream buffer,
            long lowerByteIndex = 0,
            CancellationToken ct = default)
        {
            var streamBuffer = new byte[Modules.FileModuleBase.ChunkSize];
            long sendData = 0;
            var readBufferSize = Modules.FileModuleBase.ChunkSize;

            while (true)
            {
                if (!buffer.CanRead) break;

                if (sendData + Modules.FileModuleBase.ChunkSize > response.ContentLength64) readBufferSize = (int)(response.ContentLength64 - sendData);

                buffer.Seek(lowerByteIndex + sendData, SeekOrigin.Begin);
                var read = await buffer.ReadAsync(streamBuffer, 0, readBufferSize, ct).ConfigureAwait(false);

                if (read == 0) break;

                sendData += read;
                await response.OutputStream.WriteAsync(streamBuffer, 0, readBufferSize, ct).ConfigureAwait(false);
            }
        }
    }
}