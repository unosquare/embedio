using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to serialize data to a HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="data">The data to serialize.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
    public delegate Task ResponseSerializerCallback(IHttpContext context, object? data);
}