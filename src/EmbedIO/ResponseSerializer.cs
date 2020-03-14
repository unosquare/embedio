using System;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Swan.Formatters;

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
            using var text = context.OpenResponseText(new UTF8Encoding(false));
            await text.WriteAsync(Swan.Formatters.Json.Serialize(data)).ConfigureAwait(false);
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
                using var text = context.OpenResponseText(new UTF8Encoding(false));
                await text.WriteAsync(Swan.Formatters.Json.Serialize(data, jsonSerializerCase)).ConfigureAwait(false);
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
                using var text = context.OpenResponseText(new UTF8Encoding(false));
                await text.WriteAsync(Swan.Formatters.Json.Serialize(data, serializerOptions)).ConfigureAwait(false);
            };
        }
    }
}