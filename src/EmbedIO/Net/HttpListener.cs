using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;

namespace EmbedIO.Net
{
    /// <summary>
    /// The EmbedIO implementation of the standard HTTP Listener class.
    ///
    /// Based on MONO HttpListener class.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class HttpListener : IHttpListener
    {
        private readonly SemaphoreSlim _ctxQueueSem = new SemaphoreSlim(0);
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _ctxQueue;
        private readonly ConcurrentDictionary<HttpConnection, object> _connections;
        private readonly HttpListenerPrefixCollection _prefixes;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener" /> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public HttpListener(X509Certificate certificate =null)
        {
            Certificate = certificate;

            _prefixes = new HttpListenerPrefixCollection(this);
            _connections = new ConcurrentDictionary<HttpConnection, object>();
            _ctxQueue = new ConcurrentDictionary<Guid, HttpListenerContext>();
        }
        
        /// <inheritdoc />
        public bool IgnoreWriteExceptions { get; set; }

        /// <inheritdoc />
        public bool IsListening { get; private set; }

        /// <summary>
        /// Gets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        internal X509Certificate Certificate { get; }

        /// <inheritdoc />
        public string Name { get; } = "Unosquare HTTP Listener";

        /// <inheritdoc />
        public List<string> Prefixes => _prefixes.ToList();
        
        /// <inheritdoc />
        public void Start()
        {
            if (IsListening)
                return;

            EndPointManager.AddListener(this).GetAwaiter().GetResult();
            IsListening = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            IsListening = false;
            Close(false);
        }

        /// <inheritdoc />
        public void AddPrefix(string urlPrefix) => _prefixes.Add(urlPrefix);

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            Close(true);
            _disposed = true;
        }

        /// <inheritdoc />
        public async Task<IHttpContext> GetContextAsync(CancellationToken ct)
        {
            while (true)
            {
                await _ctxQueueSem.WaitAsync(ct).ConfigureAwait(false);

                foreach (var key in _ctxQueue.Keys)
                {
                    if (_ctxQueue.TryRemove(key, out var context))
                    {
                        return context;
                    }

                    break;
                }
            }
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            if (!_ctxQueue.TryAdd(context.Id, context))
                throw new InvalidOperationException("Unable to register context");

            _ctxQueueSem.Release();
        }

        internal void UnregisterContext(HttpListenerContext context) => _ctxQueue.TryRemove(context.Id, out _);

        internal void AddConnection(HttpConnection cnc) => _connections[cnc] = cnc;

        internal void RemoveConnection(HttpConnection cnc) => _connections.TryRemove(cnc, out _);

        private void Close(bool closeExisting)
        {
            EndPointManager.RemoveListener(this).GetAwaiter().GetResult();

            var keys = _connections.Keys;
            var connections = new HttpConnection[keys.Count];
            keys.CopyTo(connections, 0);
            _connections.Clear();
            var list = new List<HttpConnection>(connections);

            for (var i = list.Count - 1; i >= 0; i--)
                list[i].Close(true);

            if (!closeExisting) return;

            while (_ctxQueue.IsEmpty == false)
            {
                foreach (var key in _ctxQueue.Keys.Select(x => x).ToList())
                {
                    if (_ctxQueue.TryGetValue(key, out var context))
                        context.Connection.Close(true);
                }
            }
        }
    }
}