using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// Asynchronously retrieves the request body as an array of <see langword="byte"/>s.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be an array of <see cref="byte"/>s containing the request body.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<byte[]> GetRequestBodyAsByteArrayAsync(
            this IHttpContext @this,
            CancellationToken cancellationToken)
        {
            using (var buffer = new MemoryStream())
            using (var stream = @this.OpenRequestStream())
            {
                await stream.CopyToAsync(buffer, WebServer.StreamCopyBufferSize, cancellationToken).ConfigureAwait(false);
                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously buffers the request body into a read-only <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be a read-only <see cref="MemoryStream"/> containing the request body.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<MemoryStream> GetRequestBodyAsMemoryStreamAsync(
            this IHttpContext @this,
            CancellationToken cancellationToken)
            => new MemoryStream(
                await GetRequestBodyAsByteArrayAsync(@this, cancellationToken).ConfigureAwait(false),
                false);

        /// <summary>
        /// Asynchronously retrieves the request body as a string.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be a <see langword="string"/> representation of the request body.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<string> GetRequestBodyAsStringAsync(this IHttpContext @this)
        {
            using (var reader = @this.OpenRequestText())
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// <para>Asynchronously deserializes a request body, using the default request deserializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// request parsing methods of version 2.</para>
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static Task<TData> GetRequestDataAsync<TData>(
            this IHttpContext @this,
            CancellationToken cancellationToken)
            => RequestDeserializer.Default<TData>(@this, cancellationToken);

        /// <summary>
        /// Asynchronously deserializes a request body, using the specified request deserializer.
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="deserializer">A <see cref="RequestDeserializerCallback{TData}"/> used to deserialize the request body.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="deserializer"/> is <see langword="null"/>.</exception>
        public static Task<TData> GetRequestDataAsync<TData>(
            this IHttpContext @this,
            RequestDeserializerCallback<TData> deserializer,
            CancellationToken cancellationToken)
            => Validate.NotNull(nameof(deserializer), deserializer)(@this, cancellationToken);

        /// <summary>
        /// Asynchronously parses a request body in <c>application/x-www-form-urlencoded</c> format.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be an <see cref="IReadOnlyDictionary{TKey,TValue}"/> interface associating form field names with their values.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<IReadOnlyDictionary<string, object>> GetRequestFormDataAsync(
            this IHttpContext @this,
            CancellationToken cancellationToken)
            => RequestParser.UrlEncodedFormData(@this, cancellationToken);
    }
}