namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to create a HTTP Listener.
    /// </summary>
    public interface IHttpListener : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the listener should ignore write exceptions.
        /// </summary>
        /// <value>
        /// <c>true</c> if [ignore write exceptions]; otherwise, <c>false</c>.
        /// </value>
        bool IgnoreWriteExceptions { get; set; }

        /// <summary>
        /// Gets the prefixes.
        /// </summary>
        /// <value>
        /// The prefixes.
        /// </value>
        List<string> Prefixes { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        bool IsListening { get; }

        /// <summary>
        /// Starts this listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this listener.
        /// </summary>
        void Stop();

        /// <summary>
        /// Adds the prefix.
        /// </summary>
        /// <param name="urlPrefix">The URL prefix.</param>
        void AddPrefix(string urlPrefix);

        /// <summary>
        /// Gets the HTTP context asynchronous.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// A task that represents the time delay for the HTTP Context.
        /// </returns>
        Task<IHttpContext> GetContextAsync(CancellationToken ct);
    }
}
