using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Net.Internal
{
    /// <summary>
    /// Represents a wrapper for Microsoft HTTP Listener.
    /// </summary>
    internal class SystemHttpListener : IHttpListener
    {
        private readonly System.Net.HttpListener _httpListener;

        public SystemHttpListener(System.Net.HttpListener httpListener)
        {
            _httpListener = httpListener;
        }

        /// <inheritdoc />
        public bool IgnoreWriteExceptions
        {
            get => _httpListener.IgnoreWriteExceptions;
            set => _httpListener.IgnoreWriteExceptions = value;
        }

        /// <inheritdoc />
        public List<string> Prefixes => _httpListener.Prefixes.ToList();

        /// <inheritdoc />
        public bool IsListening => _httpListener.IsListening;

        /// <inheritdoc />
        public string Name { get; } = "Microsoft HTTP Listener";

        /// <inheritdoc />
        public void Start() => _httpListener.Start();

        /// <inheritdoc />
        public void Stop() => _httpListener.Stop();

        /// <inheritdoc />
        public void AddPrefix(string urlPrefix) => _httpListener.Prefixes.Add(urlPrefix);

        /// <inheritdoc />
        public async Task<IHttpContextImpl> GetContextAsync(CancellationToken cancellationToken)
            => new SystemHttpContext(await _httpListener.GetContextAsync().ConfigureAwait(false));

        void IDisposable.Dispose() => ((IDisposable)_httpListener)?.Dispose();
    }
}