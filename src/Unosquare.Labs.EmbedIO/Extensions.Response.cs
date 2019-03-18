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
            response.AddHeader(HttpHeaders.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            response.AddHeader(HttpHeaders.LastModified,
                DateTime.UtcNow.ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo));
            response.AddHeader(HttpHeaders.CacheControl, "no-store, no-cache, must-revalidate");
            response.AddHeader(HttpHeaders.Pragma, "no-cache");
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
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.StringResponseAsync(htmlContent, Responses.HtmlContentType, null, useGzip, cancellationToken);
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
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError,
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
            string contentType = "application/json",
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
        public static async Task<bool> StringResponseAsync(
            this IHttpResponse response,
            string content,
            string contentType = "application/json",
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
            context.Response.ContentType = contentType ?? Responses.HtmlContentType;

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