namespace Unosquare.Net
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Labs.EmbedIO;

    /// <summary>
    /// The EmbedIO implementation of the standard HTTP Listener class.
    ///
    /// Based on MONO HttpListener class.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class HttpListener : IHttpListener
    {
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _ctxQueue;
        private readonly ConcurrentDictionary<HttpConnection, object> _connections;
        private readonly HttpListenerPrefixCollection _prefixes;
        private bool _disposed;
#if SSL
        IMonoTlsProvider tlsProvider;
        MSI.MonoTlsSettings tlsSettings;
        X509Certificate certificate;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        public HttpListener()
        {
            _prefixes = new HttpListenerPrefixCollection(this);
            _connections = new ConcurrentDictionary<HttpConnection, object>();
            _ctxQueue = new ConcurrentDictionary<Guid, HttpListenerContext>();
        }

#if SSL
        internal HttpListener(X509Certificate certificate, IMonoTlsProvider tlsProvider, MSI.MonoTlsSettings tlsSettings)
            : this()
        {
            this.certificate = certificate;
            this.tlsProvider = tlsProvider;
            this.tlsSettings = tlsSettings;
        }

        internal X509Certificate LoadCertificateAndKey(IPAddress addr, int port)
        {
            lock (registry)
            {
                if (certificate != null)
                    return certificate;

                // Actually load the certificate
                try
                {
                    string dirname = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string path = Path.Combine(dirname, ".mono");
                    path = Path.Combine(path, "httplistener");
                    string cert_file = Path.Combine(path, String.Format("{0}.cer", port));
                    if (!File.Exists(cert_file))
                        return null;
                    string pvk_file = Path.Combine(path, String.Format("{0}.pvk", port));
                    if (!File.Exists(pvk_file))
                        return null;
                    var cert = new X509Certificate2(cert_file);
                    cert.PrivateKey = PrivateKey.CreateFromFile(pvk_file).RSA;
                    certificate = cert;
                    return certificate;
                }
                catch
                {
                    // ignore errors
                    certificate = null;
                    return null;
                }
            }
        }
        
        internal IMonoSslStream CreateSslStream(Stream innerStream, bool ownsStream, MSI.MonoRemoteCertificateValidationCallback callback)
        {
            lock (registry)
            {
                if (tlsProvider == null)
                    tlsProvider = MonoTlsProviderFactory.GetProviderInternal();
                if (tlsSettings == null)
                    tlsSettings = MSI.MonoTlsSettings.CopyDefaultSettings();
                if (tlsSettings.RemoteCertificateValidationCallback == null)
                    tlsSettings.RemoteCertificateValidationCallback = callback;
                return tlsProvider.CreateSslStream(innerStream, ownsStream, tlsSettings);
            }
        }
#endif

        /// <summary>
        /// Gets a value indicating whether this instance is supported.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is supported; otherwise, <c>false</c>.
        /// </value>
        public static bool IsSupported => true;

        /// <inheritdoc />
        public bool IgnoreWriteExceptions { get; set; }

        /// <inheritdoc />
        public bool IsListening { get; private set; }

        /// <inheritdoc />
        public List<string> Prefixes => _prefixes.ToList();

        /// <summary>
        /// Closes this listener.
        /// </summary>
        public void Close()
        {
            if (_disposed)
                return;

            if (!IsListening)
            {
                _disposed = true;
                return;
            }

            Close(true);
            _disposed = true;
        }

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
        public async Task<IHttpContext> GetContextAsync()
        {
            while (true)
            {
                foreach (var key in _ctxQueue.Keys)
                {
                    if (_ctxQueue.TryRemove(key, out var context))
                    {
                        return context;
                    }
                }

                if (!IsListening)
                    return null;

                await Task.Delay(10);
            }
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            if (_ctxQueue.TryAdd(context.Id, context) == false)
                throw new InvalidOperationException("Unable to register context");
        }

        internal void UnregisterContext(HttpListenerContext context) => _ctxQueue.TryRemove(context.Id, out _);

        internal void AddConnection(HttpConnection cnc) => _connections[cnc] = cnc;

        internal void RemoveConnection(HttpConnection cnc) => _connections.TryRemove(cnc, out _);

        private void Close(bool closeExisting)
        {
            EndPointManager.RemoveListener(this).GetAwaiter().GetResult();

            var list = new List<HttpConnection>();

            var keys = _connections.Keys;
            var connections = new HttpConnection[keys.Count];
            keys.CopyTo(connections, 0);
            _connections.Clear();
            list.AddRange(connections);

            for (var i = list.Count - 1; i >= 0; i--)
                list[i].Close(true);

            if (closeExisting == false) return;

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