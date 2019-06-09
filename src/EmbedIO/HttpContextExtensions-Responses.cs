using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        private const string StandardHtmlHeaderFormat = "<html><head><meta charset=\"{2}\"><title>{0} - {1}</title></head><body><h1>{0} - {1}</h1>";
        private const string StandardHtmlFooter = "</body></html>";

        /// <summary>
        /// Sets a redirection status code and adds a <c>Location</c> header to the response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="location">The URL to which the user agent should be redirected.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="location"/> is not a valid relative or absolute URL.<see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        public static void Redirect(this IHttpContext @this, string location, int statusCode = (int)HttpStatusCode.Found)
        {
            location = Validate.Url(nameof(location), location, @this.Request.Url);

            if (statusCode < 300 || statusCode > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            @this.Response.SetEmptyResponse(statusCode);
            @this.Response.Headers[HttpHeaderNames.Location] = location;
        }

        /// <summary>
        /// Asynchronously sends a string as response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="content">The response content.</param>
        /// <param name="contentType">The MIME type of the content. If <see langword="null"/>, the content type will not be set.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to use.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="RequestHandlerCallback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="content"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="encoding"/> is <see langword="null"/>.</para>
        /// </exception>
        public static async Task<bool> SendStringAsync(
            this IHttpContext @this,
            string content,
            string contentType,
            Encoding encoding,
            CancellationToken cancellationToken)
        {
            content = Validate.NotNull(nameof(content), content);
            encoding = Validate.NotNull(nameof(encoding), encoding);

            if (contentType != null)
                @this.Response.ContentType = contentType;

            using (var text = @this.OpenResponseText(encoding))
                await text.WriteAsync(content).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="RequestHandlerCallback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpContext,int,Action{TextWriter},CancellationToken)"/>
        public static Task<bool> SendStandardHtmlAsync(this IHttpContext @this, int statusCode, CancellationToken cancellationToken)
            => SendStandardHtmlAsync(@this, statusCode, null, cancellationToken);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="writeAdditionalHtml">A callback function that may write additional HTML code
        /// to a <see cref="TextWriter"/> representing the response output.
        /// If not <see langword="null"/>, the callback is called immediately before closing the HTML <c>body</c> tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="RequestHandlerCallback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpContext,int,CancellationToken)"/>
        public static Task<bool> SendStandardHtmlAsync(
            this IHttpContext @this,
            int statusCode,
            Action<TextWriter> writeAdditionalHtml,
            CancellationToken cancellationToken)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.Response.StatusCode = statusCode;
            @this.Response.StatusDescription = statusDescription;
            @this.Response.ContentType = MimeTypes.HtmlType;
            using (var text = @this.OpenResponseText(Encoding.UTF8))
            {
                text.Write(StandardHtmlHeaderFormat, statusCode, statusDescription, Encoding.UTF8.WebName);
                writeAdditionalHtml?.Invoke(text);
                text.Write(StandardHtmlFooter);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// <para>Asynchronously sends serialized data as a response, using the default response serializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// response methods of version 2.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="RequestHandlerCallback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="SendDataAsync(IHttpContext,ResponseSerializerCallback,object,CancellationToken)"/>
        /// <seealso cref="ResponseSerializer.Default"/>
        public static async Task<bool> SendDataAsync(this IHttpContext @this, object data, CancellationToken cancellationToken)
        {
            await ResponseSerializer.Default(@this, data, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// <para>Asynchronously sends serialized data as a response, using the specified response serializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// response methods of version 2.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="serializer">A <see cref="ResponseSerializerCallback"/> used to prepare the response.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="RequestHandlerCallback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="serializer"/> is <see langword="null"/>.</exception>
        /// <seealso cref="SendDataAsync(IHttpContext,ResponseSerializerCallback,object,CancellationToken)"/>
        /// <seealso cref="ResponseSerializer.Default"/>
        public static async Task<bool> SendDataAsync(
            this IHttpContext @this,
            ResponseSerializerCallback serializer,
            object data,
            CancellationToken cancellationToken)
        {
            await Validate.NotNull(nameof(serializer), serializer)(@this, data, cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
    }
}