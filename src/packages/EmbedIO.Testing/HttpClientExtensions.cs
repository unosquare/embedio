﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Utilities;

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
        public static async Task<HttpResponseMessage> HeadAsync([ValidatedNotNull] this HttpClient @this, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            return await @this.SendAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sends a <c>HEAD</c> request to a specified URL.
        /// </summary>
        /// <param name="this">The <see cref="HttpClient"/> on which this method is called.</param>
        /// <param name="url">The request URL.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be a <see cref="HttpResponseMessage"/>.</returns>
        public static async Task<HttpResponseMessage> HeadAsync([ValidatedNotNull] this HttpClient @this, Uri url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            return await @this.SendAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sends an <c>OPTIONS</c> request to a specified URL.
        /// </summary>
        /// <param name="this">The <see cref="HttpClient"/> on which this method is called.</param>
        /// <param name="url">The request URL.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be a <see cref="HttpResponseMessage"/>.</returns>
        public static async Task<HttpResponseMessage> OptionsAsync([ValidatedNotNull] this HttpClient @this, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, url);
            return await @this.SendAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sends an <c>OPTIONS</c> request to a specified URL.
        /// </summary>
        /// <param name="this">The <see cref="HttpClient"/> on which this method is called.</param>
        /// <param name="url">The request URL.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be a <see cref="HttpResponseMessage"/>.</returns>
        public static async Task<HttpResponseMessage> OptionsAsync([ValidatedNotNull] this HttpClient @this, Uri url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, url);
            return await @this.SendAsync(request).ConfigureAwait(false);
        }
    }
}