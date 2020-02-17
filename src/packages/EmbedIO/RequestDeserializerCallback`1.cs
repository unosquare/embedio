using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to deserialize an HTTP request body.
    /// </summary>
    /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
    /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
    /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
    /// whose result will be the deserialized data.</returns>
    public delegate Task<TData> RequestDeserializerCallback<TData>(IHttpContext context);
}