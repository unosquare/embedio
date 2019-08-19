using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Testing
{
    /// <summary>
    /// Provides extension methods for <see cref="HttpClient"/>.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Asynchronously sends a <c>HEAD</c> request to a specified URL.
        /// </summary>
        /// <param name="this">The <see cref="HttpClient"/> on which this method is called.</param>
        /// <param name="url">The request URL.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be a <see cref="HttpResponseMessage"/>.</returns>
        public static Task<HttpResponseMessage> HeadAsync(this HttpClient @this, string url)
            => @this.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

        /// <summary>
        /// Asynchronously sends an <c>OPTIONS</c> request to a specified URL.
        /// </summary>
        /// <param name="this">The <see cref="HttpClient"/> on which this method is called.</param>
        /// <param name="url">The request URL.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be a <see cref="HttpResponseMessage"/>.</returns>
        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient @this, string url)
            => @this.SendAsync(new HttpRequestMessage(HttpMethod.Options, url));
    }
}