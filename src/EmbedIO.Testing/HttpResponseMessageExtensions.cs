using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Testing
{
    /// <summary>
    /// Provides extension methods for <see cref="HttpResponseMessage"/>
    /// and tasks returning instances of <see cref="HttpResponseMessage"/>.
    /// </summary>
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Asynchronously gets a HTTP response body as a string.
        /// </summary>
        /// <param name="this">The <see cref="Task{TResult}"/> that will return the response.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be the response body as a string.</returns>
        public static async Task<string?> ReceiveStringAsync(this Task<HttpResponseMessage> @this)
        {
            using var response = await @this.ConfigureAwait(false);
            if (response == null) return null;
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}