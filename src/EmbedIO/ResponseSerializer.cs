using System.Text;
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
        /// <para>Equivalent to <see cref="Json"/>.</para>
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
    }
}