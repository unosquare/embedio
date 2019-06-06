using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to deserialize a HTTP request body.
    /// </summary>
    /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
    /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
    /// whose result will be the deserialized data.</returns>
    public delegate Task<TData> RequestDeserializerCallback<TData>(IHttpContext context, CancellationToken cancellationToken);
}