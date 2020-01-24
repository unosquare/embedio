using System.Threading.Tasks;

namespace EmbedIO.Files
{
    /// <summary>
    /// A callback used to handle a request in <see cref="FileModule"/>.
    /// </summary>
    /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
    /// <param name="info">If the requested path has been successfully mapped to a resource (file or directory), the result of the mapping;
    /// otherwise, <see langword="null"/>.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
    public delegate Task FileRequestHandlerCallback(IHttpContext context, MappedResourceInfo? info);
}