using System;
using System.Threading.Tasks;
using Swan;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard request deserialization callbacks.
    /// </summary>
    public static class RequestDeserializer
    {
        /// <summary>
        /// <para>The default request deserializer used by EmbedIO.</para>
        /// <para>Equivalent to <see cref="Json{TData}"/>.</para>
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        public static Task<TData> Default<TData>(IHttpContext context) => Json<TData>(context);

        /// <summary>
        /// Asynchronously deserializes a request body in JSON format.
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        public static async Task<TData> Json<TData>(IHttpContext context)
        {
            string body;
            using (var reader = context.OpenRequestText())
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            try
            {
                return Swan.Formatters.Json.Deserialize<TData>(body);
            }
            catch (FormatException)
            {
                $"[{context.Id}] Cannot convert JSON request body to {typeof(TData).Name}, sending 400 Bad Request..."
                    .Warn($"{nameof(RequestDeserializer)}.{nameof(Json)}");

                throw HttpException.BadRequest("Incorrect request data format.");
            }
        }
    }
}