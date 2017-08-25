#if !NET47
//
// System.Net.HttpListener
//
// Authors:
// Gonzalo Paniagua Javier (gonzalo@novell.com)
// Marek Safar (marek.safar@gmail.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
// Copyright 2011 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
namespace Unosquare.Net
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    /// <summary>
    /// The MONO implementation of the standard Http Listener class
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class HttpListener : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, HttpListenerContext> _ctxQueue;
        private readonly Hashtable _connections;
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
            Prefixes = new HttpListenerPrefixCollection(this);
            _connections = Hashtable.Synchronized(new Hashtable());
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

        /// <summary>
        /// Gets or sets a value indicating whether the listener should ignore write exceptions.
        /// </summary>
        /// <value>
        /// <c>true</c> if [ignore write exceptions]; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreWriteExceptions { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is listening.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening { get; private set; }

        /// <summary>
        /// Gets the prefixes.
        /// </summary>
        /// <value>
        /// The prefixes.
        /// </value>
        public HttpListenerPrefixCollection Prefixes { get; }

        /// <summary>
        /// Gets or sets the realm.
        /// </summary>
        /// <value>
        /// The realm.
        /// </value>
        public string Realm { get; set; }

        /// <summary>
        /// Aborts this listener.
        /// </summary>
        /// <returns>The task aborting</returns>
        public Task AbortAsync() => CloseAsync();

        /// <summary>
        /// Closes this listener.
        /// </summary>
        /// <returns>The task closing</returns>
        public async Task CloseAsync()
        {
            if (_disposed)
                return;

            if (!IsListening)
            {
                _disposed = true;
                return;
            }

            await CloseAsync(true);
            _disposed = true;
        }

        /// <summary>
        /// Starts this listener.
        /// </summary>
        public void Start()
        {
            if (IsListening)
                return;

            EndPointManager.AddListener(this);
            IsListening = true;
        }

        /// <summary>
        /// Stops this listener.
        /// </summary>
        /// <returns>The task stopping the listener</returns>
        public Task StopAsync()
        {
            IsListening = false;
            return CloseAsync(false);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;
            
            CloseAsync(true).Wait();
            _disposed = true;
        }

        /// <summary>
        /// Gets the HTTP context asynchronously.
        /// </summary>
        /// <returns>A task that represents the time delay for the httpListenerContext</returns>
        public async Task<HttpListenerContext> GetContextAsync()
        {
            while (true)
            {
                foreach (var key in _ctxQueue.Keys)
                {
                    if (_ctxQueue.TryRemove(key, out HttpListenerContext context))
                        return context;
                }

                await Task.Delay(10);
            }
        }
        
        internal void RegisterContext(HttpListenerContext context)
        {
            if (_ctxQueue.TryAdd(context.Id, context) == false)
                throw new Exception("Unable to register context");
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            _ctxQueue.TryRemove(context.Id, out HttpListenerContext removedContext);
        }

        internal void AddConnection(HttpConnection cnc)
        {
            _connections[cnc] = cnc;
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            _connections.Remove(cnc);
        }

        private async Task CloseAsync(bool closeExisting)
        {
            EndPointManager.RemoveListener(this);

            var conns = new List<HttpConnection>();

            lock (_connections.SyncRoot)
            {
                var keys = _connections.Keys;
                var connsArray = new HttpConnection[keys.Count];
                keys.CopyTo(connsArray, 0);
                _connections.Clear();
                conns.AddRange(connsArray);
            }

            for (var i = conns.Count - 1; i >= 0; i--)
                await conns[i].CloseAsync(true).ConfigureAwait(false);

            if (closeExisting == false) return;

            while (_ctxQueue.IsEmpty == false)
            {
                foreach (var key in _ctxQueue.Keys.Select(x => x).ToList())
                {
                    if (_ctxQueue.TryGetValue(key, out HttpListenerContext context))
                        await context.Connection.CloseAsync(true).ConfigureAwait(false);
                }
            }
        }
    }
}
#endif