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
        {
            // System.Net.HttpListener.GetContextAsync may throw ObjectDisposedException
            // when stopping a WebServer. This has been observed on Mono 5.20.1.19
            // on Raspberry Pi, but the fact remains that the method does not take
            // a CancellationToken as parameter, and WebServerBase<>.RunAsync counts on it.
            System.Net.HttpListenerContext context;
            try
            {
                context = await _httpListener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    "Probable cancellation detected by catching an exception in System.Net.HttpListener.GetContextAsync",
                    e,
                    cancellationToken);
            }

            return new SystemHttpContext(context);
        }

        void IDisposable.Dispose() => ((IDisposable)_httpListener)?.Dispose();
    }
}