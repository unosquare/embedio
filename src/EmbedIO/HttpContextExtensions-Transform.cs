using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {

        /// <summary>
        /// Parses the request body as JSON, applies a transformation function,
        /// and sends the result as a JSON response.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="this">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJsonAsync<TIn, TOut>(
            this IHttpContext @this,
            Func<TIn, CancellationToken, Task<TOut>> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await @this.Request.ParseJsonAsync<TIn>().ConfigureAwait(false);
            var responseJson = await transformFunc(requestJson, cancellationToken).ConfigureAwait(false);
            return await @this.SendDataAsync(responseJson, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="this">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJsonAsync<TIn, TOut>(
            this IHttpContext @this,
            Func<TIn, TOut> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await @this.Request.ParseJsonAsync<TIn>().ConfigureAwait(false);
            var responseJson = transformFunc(requestJson);
            return await @this.SendDataAsync(responseJson, cancellationToken).ConfigureAwait(false);
        }
    }
}