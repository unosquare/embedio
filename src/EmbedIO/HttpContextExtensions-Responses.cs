using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        private const string StandardHtmlHeaderFormat = "<html><head><meta charset=\"{2}\"><title>{0} - {1}</title></head><body><h1>{0} - {1}</h1>";
        private const string StandardHtmlFooter = "</body></html>";

        /// <summary>
        /// Asynchronously sends a string as response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="content">The response content.</param>
        /// <param name="contentType">The MIME type of the content. If <see langword="null"/>, the content type will not be set.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to use.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="content"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="encoding"/> is <see langword="null"/>.</para>
        /// </exception>
        public static async Task SendStringAsync(
            this IHttpContext @this,
            string content,
            string contentType,
            Encoding encoding)
        {
            content = Validate.NotNull(nameof(content), content);
            encoding = Validate.NotNull(nameof(encoding), encoding);

            if (contentType != null)
            {
                @this.Response.ContentType = contentType;
                @this.Response.ContentEncoding = encoding;
            }

            using var text = @this.OpenResponseText(encoding);
            await text.WriteAsync(content).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpContext,int,Action{TextWriter})"/>
        public static Task SendStandardHtmlAsync(this IHttpContext @this, int statusCode)
            => SendStandardHtmlAsync(@this, statusCode, null);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="writeAdditionalHtml">A callback function that may write additional HTML code
        /// to a <see cref="TextWriter"/> representing the response output.
        /// If not <see langword="null"/>, the callback is called immediately before closing the HTML <c>body</c> tag.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpContext,int)"/>
        public static Task SendStandardHtmlAsync(
            this IHttpContext @this,
            int statusCode,
            Action<TextWriter>? writeAdditionalHtml)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.Response.StatusCode = statusCode;
            @this.Response.StatusDescription = statusDescription;
            @this.Response.ContentType = MimeType.Html;
            @this.Response.ContentEncoding = WebServer.DefaultEncoding;
            using (var text = @this.OpenResponseText(WebServer.DefaultEncoding))
            {
                text.Write(StandardHtmlHeaderFormat, statusCode, statusDescription, WebServer.DefaultEncoding.WebName);
                writeAdditionalHtml?.Invoke(text);
                text.Write(StandardHtmlFooter);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// <para>Asynchronously sends serialized data as a response, using the default response serializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// response methods of version 2.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="data">The data to serialize.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="SendDataAsync(IHttpContext,ResponseSerializerCallback,object)"/>
        /// <seealso cref="ResponseSerializer.Default"/>
        public static Task SendDataAsync(this IHttpContext @this, object data)
            => ResponseSerializer.Default(@this, data);

        /// <summary>
        /// <para>Asynchronously sends serialized data as a response, using the specified response serializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// response methods of version 2.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="serializer">A <see cref="ResponseSerializerCallback"/> used to prepare the response.</param>
        /// <param name="data">The data to serialize.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/>.</exception>
        /// <seealso cref="SendDataAsync(IHttpContext,ResponseSerializerCallback,object)"/>
        /// <seealso cref="ResponseSerializer.Default"/>
        public static Task SendDataAsync(this IHttpContext @this, ResponseSerializerCallback serializer, object data)
            => Validate.NotNull(nameof(serializer), serializer)(@this, data);
    }
}