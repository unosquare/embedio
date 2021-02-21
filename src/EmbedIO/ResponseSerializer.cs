using EmbedIO.Utilities;
using Swan.Formatters;
using System;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard response serializer callbacks.
    /// </summary>
    /// <seealso cref="ResponseSerializerCallback"/>
    public static class ResponseSerializer
    {
        /// <summary>
        /// <para>The default response serializer callback used by EmbedIO.</para>
        /// <para>Equivalent to <see cref="Json(EmbedIO.IHttpContext,object?)">Json</see>.</para>
        /// </summary>
        public static readonly ResponseSerializerCallback Default = Json;

        private static readonly ResponseSerializerCallback ChunkedEncodingBaseSerializer = GetBaseSerializer(false);
        private static readonly ResponseSerializerCallback BufferingBaseSerializer = GetBaseSerializer(true);

        /// <summary>
        /// Serializes data in JSON format to a HTTP response,
        /// using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <param name="data">The data to serialize.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        public static async Task Json(IHttpContext context, object? data)
        {
            context.Response.ContentType = MimeType.Json;
            context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
            await ChunkedEncodingBaseSerializer(context, Swan.Formatters.Json.Serialize(data)).ConfigureAwait(false);
        }

        /// <summary>
        /// Serializes data in JSON format with the specified <paramref name="jsonSerializerCase"/>
        /// to a HTTP response, using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="jsonSerializerCase">The JSON serializer case.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize
        /// data to a HTTP response.</returns>
        public static ResponseSerializerCallback Json(JsonSerializerCase jsonSerializerCase)
            => async (context, data) => {
                context.Response.ContentType = MimeType.Json;
                context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
                await ChunkedEncodingBaseSerializer(context, Swan.Formatters.Json.Serialize(data, jsonSerializerCase))
                    .ConfigureAwait(false);
            };

        /// <summary>
        /// Serializes data in JSON format with the specified <paramref name="serializerOptions"/>
        /// to a HTTP response, using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="serializerOptions">The JSON serializer options.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize
        /// data to a HTTP response.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="serializerOptions"/> is <see langword="null"/>.
        /// </exception>
        public static ResponseSerializerCallback Json(SerializerOptions serializerOptions)
        {
            _ = Validate.NotNull(nameof(serializerOptions), serializerOptions);
            
            return async (context, data) => {
                context.Response.ContentType = MimeType.Json;
                context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
                await ChunkedEncodingBaseSerializer(context, Swan.Formatters.Json.Serialize(data, serializerOptions))
                    .ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Serializes data in JSON format to a HTTP response, using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="bufferResponse"><see langword="true"/> to write the response body to a memory buffer first,
        /// then send it all together with a <c>Content-Length</c> header; <see langword="false"/> to use chunked
        /// transfer encoding.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize
        /// data to a HTTP response.</returns>
        public static ResponseSerializerCallback Json(bool bufferResponse)
            => async (context, data) => {
                context.Response.ContentType = MimeType.Json;
                context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
                var baseSerializer = None(bufferResponse);
                await baseSerializer(context, Swan.Formatters.Json.Serialize(data))
                    .ConfigureAwait(false);
            };

        /// <summary>
        /// Serializes data in JSON format with the specified <paramref name="jsonSerializerCase"/>
        /// to a HTTP response, using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="bufferResponse"><see langword="true"/> to write the response body to a memory buffer first,
        /// then send it all together with a <c>Content-Length</c> header; <see langword="false"/> to use chunked
        /// transfer encoding.</param>
        /// <param name="jsonSerializerCase">The JSON serializer case.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize
        /// data to a HTTP response.</returns>
        public static ResponseSerializerCallback Json(bool bufferResponse, JsonSerializerCase jsonSerializerCase)
            => async (context, data) => {
                context.Response.ContentType = MimeType.Json;
                context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
                var baseSerializer = None(bufferResponse);
                await baseSerializer(context, Swan.Formatters.Json.Serialize(data, jsonSerializerCase))
                    .ConfigureAwait(false);
            };

        /// <summary>
        /// Serializes data in JSON format with the specified <paramref name="serializerOptions"/>
        /// to a HTTP response, using the <see cref="Swan.Formatters.Json"/> utility class.
        /// </summary>
        /// <param name="bufferResponse"><see langword="true"/> to write the response body to a memory buffer first,
        /// then send it all together with a <c>Content-Length</c> header; <see langword="false"/> to use chunked
        /// transfer encoding.</param>
        /// <param name="serializerOptions">The JSON serializer options.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize
        /// data to a HTTP response.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="serializerOptions"/> is <see langword="null"/>.
        /// </exception>
        public static ResponseSerializerCallback Json(bool bufferResponse, SerializerOptions serializerOptions)
        {
            _ = Validate.NotNull(nameof(serializerOptions), serializerOptions);
            
            return async (context, data) => {
                context.Response.ContentType = MimeType.Json;
                context.Response.ContentEncoding = WebServer.Utf8NoBomEncoding;
                var baseSerializer = None(bufferResponse);
                await baseSerializer(context, Swan.Formatters.Json.Serialize(data, serializerOptions))
                    .ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Sends data in a HTTP response without serialization.
        /// </summary>
        /// <param name="bufferResponse"><see langword="true"/> to write the response body to a memory buffer first,
        /// then send it all together with a <c>Content-Length</c> header; <see langword="false"/> to use chunked
        /// transfer encoding.</param>
        /// <returns>A <see cref="ResponseSerializerCallback"/> that can be used to serialize data to a HTTP response.</returns>
        /// <remarks>
        /// <para><see cref="string"/>s and one-dimensional arrays of <see cref="byte"/>s
        /// are sent to the client unchanged; every other type is converted to a string.</para>
        /// <para>The <see cref="IHttpResponse.ContentType">ContentType</see> set on the response is used to negotiate
        /// a compression method, according to request headers.</para>
        /// <para>Strings (and other types converted to strings) are sent with the encoding specified by <see cref="IHttpResponse.ContentEncoding"/>.</para>
        /// </remarks>
        public static ResponseSerializerCallback None(bool bufferResponse)
            => bufferResponse ? BufferingBaseSerializer : ChunkedEncodingBaseSerializer;

        private static ResponseSerializerCallback GetBaseSerializer(bool bufferResponse)
            => async (context, data) => {
                if (data is null)
                {
                    return;
                }

                var isBinaryResponse = data is byte[];

                if (!context.TryDetermineCompression(context.Response.ContentType, out var preferCompression))
                {
                    preferCompression = true;
                }

                if (isBinaryResponse)
                {
                    var responseBytes = (byte[])data;
                    using var stream = context.OpenResponseStream(bufferResponse, preferCompression);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                }
                else
                {
                    var responseString = data is string stringData ? stringData : data.ToString() ?? string.Empty;
                    using var text = context.OpenResponseText(context.Response.ContentEncoding, bufferResponse, preferCompression);
                    await text.WriteAsync(responseString).ConfigureAwait(false);
                }
            };
    }
}