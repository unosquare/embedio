using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        private static readonly object FormDataKey = new object();
        private static readonly object QueryDataKey = new object();

        /// <summary>
        /// Asynchronously retrieves the request body as an array of <see langword="byte"/>s.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be an array of <see cref="byte"/>s containing the request body.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<byte[]> GetRequestBodyAsByteArrayAsync(this IHttpContext @this)
        {
            using var buffer = new MemoryStream();
            using var stream = @this.OpenRequestStream();
            await stream.CopyToAsync(buffer, WebServer.StreamCopyBufferSize, @this.CancellationToken).ConfigureAwait(false);
            return buffer.ToArray();
        }

        /// <summary>
        /// Asynchronously buffers the request body into a read-only <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be a read-only <see cref="MemoryStream"/> containing the request body.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static async Task<MemoryStream> GetRequestBodyAsMemoryStreamAsync(this IHttpContext @this)
            => new MemoryStream(
                await GetRequestBodyAsByteArrayAsync(@this).ConfigureAwait(false),
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
            using var reader = @this.OpenRequestText();
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// <para>Asynchronously deserializes a request body, using the default request deserializer.</para>
        /// <para>As of EmbedIO version 3.0, the default response serializer has the same behavior of JSON
        /// request parsing methods of version 2.</para>
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static Task<TData> GetRequestDataAsync<TData>(this IHttpContext @this)
            => RequestDeserializer.Default<TData>(@this);

        /// <summary>
        /// Asynchronously deserializes a request body, using the specified request deserializer.
        /// </summary>
        /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="deserializer">A <see cref="RequestDeserializerCallback{TData}"/> used to deserialize the request body.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be the deserialized data.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="deserializer"/> is <see langword="null"/>.</exception>
        public static Task<TData> GetRequestDataAsync<TData>(this IHttpContext @this,RequestDeserializerCallback<TData> deserializer)
            => Validate.NotNull(nameof(deserializer), deserializer)(@this);

        /// <summary>
        /// Asynchronously parses a request body in <c>application/x-www-form-urlencoded</c> format.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be a read-only <see cref="NameValueCollection"/>of form field names and values.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>This method may safely be called more than once for the same <see cref="IHttpContext"/>:
        /// it will return the same collection instead of trying to parse the request body again.</para>
        /// </remarks>
        public static async Task<NameValueCollection> GetRequestFormDataAsync(this IHttpContext @this)
        {
            if (!@this.Items.TryGetValue(FormDataKey, out var previousResult))
            {
                NameValueCollection result;
                try
                {
                    using var reader = @this.OpenRequestText();
                    result = UrlEncodedDataParser.Parse(await reader.ReadToEndAsync().ConfigureAwait(false), false);
                }
                catch (Exception e)
                {
                    @this.Items[FormDataKey] = e;
                    throw;
                }

                @this.Items[FormDataKey] = result;
                return result;
            }

            switch (previousResult)
            {
                case NameValueCollection collection:
                    return collection;

                case Exception exception:
                    throw exception.RethrowPreservingStackTrace();

                case null:
                    throw SelfCheck.Failure($"Previous result of {nameof(HttpContextExtensions)}.{nameof(GetRequestFormDataAsync)} is null.");

                default:
                    throw SelfCheck.Failure($"Previous result of {nameof(HttpContextExtensions)}.{nameof(GetRequestFormDataAsync)} is of unexpected type {previousResult.GetType().FullName}");
            }
        }

        /// <summary>
        /// Parses a request URL query. Note that this is different from getting the <see cref="IHttpRequest.QueryString"/> property,
        /// in that fields without an equal sign are treated as if they have an empty value, instead of their keys being grouped
        /// as values of the <c>null</c> key.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>A read-only <see cref="NameValueCollection"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>This method may safely be called more than once for the same <see cref="IHttpContext"/>:
        /// it will return the same collection instead of trying to parse the request body again.</para>
        /// </remarks>
        public static NameValueCollection GetRequestQueryData(this IHttpContext @this)
        {
            if (!@this.Items.TryGetValue(QueryDataKey, out var previousResult))
            {
                NameValueCollection result;
                try
                {
                    result = UrlEncodedDataParser.Parse(@this.Request.Url.Query, false);
                }
                catch (Exception e)
                {
                    @this.Items[FormDataKey] = e;
                    throw;
                }

                @this.Items[FormDataKey] = result;
                return result;
            }

            switch (previousResult)
            {
                case NameValueCollection collection:
                    return collection;

                case Exception exception:
                    throw exception.RethrowPreservingStackTrace();

                case null:
                    throw SelfCheck.Failure($"Previous result of {nameof(HttpContextExtensions)}.{nameof(GetRequestQueryData)} is null.");

                default:
                    throw SelfCheck.Failure($"Previous result of {nameof(HttpContextExtensions)}.{nameof(GetRequestQueryData)} is of unexpected type {previousResult.GetType().FullName}");
            }
        }
    }
}