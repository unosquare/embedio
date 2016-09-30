#if !NET452
//
// System.Net.HttpListener
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//	Marek Safar (marek.safar@gmail.com)
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
//

using System.Collections;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;

namespace System.Net
{
    public delegate AuthenticationSchemes AuthenticationSchemeSelector(HttpListenerRequest httpRequest);

    public sealed class HttpListener : IDisposable
    {
        AuthenticationSchemes _authSchemes;
        readonly HttpListenerPrefixCollection _prefixes;
        AuthenticationSchemeSelector _authSelector;
        string _realm;
        bool _ignoreWriteExceptions;
        bool _unsafeNtlmAuth;
        bool _disposed;
#if SSL
        IMonoTlsProvider tlsProvider;
        MSI.MonoTlsSettings tlsSettings;
        X509Certificate certificate;
#endif

        readonly Hashtable _registry;   // Dictionary<HttpListenerContext,HttpListenerContext> 
        readonly ArrayList _ctxQueue;  // List<HttpListenerContext> ctx_queue;
        readonly ArrayList _waitQueue; // List<ListenerAsyncResult> wait_queue;
        readonly Hashtable _connections;

        //ServiceNameStore defaultServiceNames;
        //ExtendedProtectionPolicy _extendedProtectionPolicy;
        ExtendedProtectionSelector _extendedProtectionSelectorDelegate;

        public delegate ExtendedProtectionPolicy ExtendedProtectionSelector(HttpListenerRequest request);

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        public HttpListener()
        {
            _prefixes = new HttpListenerPrefixCollection(this);
            _registry = new Hashtable();
            _connections = Hashtable.Synchronized(new Hashtable());
            _ctxQueue = new ArrayList();
            _waitQueue = new ArrayList();
            _authSchemes = AuthenticationSchemes.Anonymous;
            //defaultServiceNames = new ServiceNameStore();
            //_extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
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
        // TODO: Digest, NTLM and Negotiate require ControlPrincipal
        public AuthenticationSchemes AuthenticationSchemes
        {
            get { return _authSchemes; }
            set
            {
                CheckDisposed();
                _authSchemes = value;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get { return _authSelector; }
            set
            {
                CheckDisposed();
                _authSelector = value;
            }
        }

        //public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        //{
        //    get { return extendedProtectionSelectorDelegate; }
        //    set
        //    {
        //        CheckDisposed();
        //        if (value == null)
        //            throw new ArgumentNullException();

        //        if (!AuthenticationManager.OSSupportsExtendedProtection)
        //            throw new PlatformNotSupportedException(SR.GetString(SR.security_ExtendedProtection_NoOSSupport));

        //        extendedProtectionSelectorDelegate = value;
        //    }
        //}

        public bool IgnoreWriteExceptions
        {
            get { return _ignoreWriteExceptions; }
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public bool IsListening { get; private set; }

        public static bool IsSupported => true;

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return _prefixes;
            }
        }
        
        public string Realm
        {
            get { return _realm; }
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }
        
        public bool UnsafeConnectionNtlmAuthentication
        {
            get { return _unsafeNtlmAuth; }
            set
            {
                CheckDisposed();
                _unsafeNtlmAuth = value;
            }
        }

        public void Abort()
        {
            if (_disposed)
                return;

            if (!IsListening)
            {
                return;
            }

            Close(true);
        }

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

        void Close(bool force)
        {
            CheckDisposed();
            EndPointManager.RemoveListener(this);
            Cleanup(force);
        }

        void Cleanup(bool closeExisting)
        {
            lock (_registry)
            {
                if (closeExisting)
                {
                    // Need to copy this since closing will call UnregisterContext
                    var keys = _registry.Keys;
                    var all = new HttpListenerContext[keys.Count];
                    keys.CopyTo(all, 0);
                    _registry.Clear();
                    for (var i = all.Length - 1; i >= 0; i--)
                        all[i].Connection.Close(true);
                }

                lock (_connections.SyncRoot)
                {
                    var keys = _connections.Keys;
                    var conns = new HttpConnection[keys.Count];
                    keys.CopyTo(conns, 0);
                    _connections.Clear();
                    for (var i = conns.Length - 1; i >= 0; i--)
                        conns[i].Close(true);
                }
                lock (_ctxQueue)
                {
                    var ctxs = (HttpListenerContext[])_ctxQueue.ToArray(typeof(HttpListenerContext));
                    _ctxQueue.Clear();
                    for (var i = ctxs.Length - 1; i >= 0; i--)
                        ctxs[i].Connection.Close(true);
                }

                lock (_waitQueue)
                {
                    Exception exc = new ObjectDisposedException("listener");
                    foreach (ListenerAsyncResult ares in _waitQueue)
                    {
                        ares.Complete(exc);
                    }
                    _waitQueue.Clear();
                }
            }
        }

        public IAsyncResult BeginGetContext(AsyncCallback callback, object state)
        {
            CheckDisposed();
            if (!IsListening)
                throw new InvalidOperationException("Please, call Start before using this method.");

            var ares = new ListenerAsyncResult(callback, state);

            // lock wait_queue early to avoid race conditions
            lock (_waitQueue)
            {
                lock (_ctxQueue)
                {
                    var ctx = GetContextFromQueue();
                    if (ctx != null)
                    {
                        ares.Complete(ctx, true);
                        return ares;
                    }
                }

                _waitQueue.Add(ares);
            }

            return ares;
        }

        public HttpListenerContext EndGetContext(IAsyncResult asyncResult)
        {
            if (_disposed) return null;
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            var ares = asyncResult as ListenerAsyncResult;
            if (ares == null)
                throw new ArgumentException("Wrong IAsyncResult.", nameof(asyncResult));
            if (ares.EndCalled)
                throw new ArgumentException("Cannot reuse this IAsyncResult");
            ares.EndCalled = true;

            if (!ares.IsCompleted)
                ares.AsyncWaitHandle.WaitOne();

            lock (_waitQueue)
            {
                var idx = _waitQueue.IndexOf(ares);
                if (idx >= 0)
                    _waitQueue.RemoveAt(idx);
            }

            var context = ares.GetContext();
            context.ParseAuthentication(SelectAuthenticationScheme(context));
            return context; // This will throw on error.
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerContext context)
        {
            return AuthenticationSchemeSelectorDelegate?.Invoke(context.Request) ?? _authSchemes;
        }

        public HttpListenerContext GetContext()
        {
            // The prefixes are not checked when using the async interface!?
            if (_prefixes.Count == 0)
                throw new InvalidOperationException("Please, call AddPrefix before using this method.");

            var ares = (ListenerAsyncResult)BeginGetContext(null, null);
            ares.InGet = true;
            return EndGetContext(ares);
        }

        public void Start()
        {
            CheckDisposed();
            if (IsListening)
                return;

            EndPointManager.AddListener(this);
            IsListening = true;
        }

        public void Stop()
        {
            CheckDisposed();
            IsListening = false;
            Close(false);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            Close(true); //TODO: Should we force here or not?
            _disposed = true;
        }

        public Task<HttpListenerContext> GetContextAsync()
        {
            return Task<HttpListenerContext>.Factory.FromAsync(BeginGetContext, EndGetContext, null);
        }

        internal void CheckDisposed()
        {
            //if (disposed)
            //    throw new ObjectDisposedException(GetType().ToString());
        }

        // Must be called with a lock on ctx_queue
        HttpListenerContext GetContextFromQueue()
        {
            if (_ctxQueue.Count == 0)
                return null;

            var context = (HttpListenerContext)_ctxQueue[0];
            _ctxQueue.RemoveAt(0);
            return context;
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            lock (_registry)
                _registry[context] = context;

            ListenerAsyncResult ares = null;
            lock (_waitQueue)
            {
                if (_waitQueue.Count == 0)
                {
                    lock (_ctxQueue)
                        _ctxQueue.Add(context);
                }
                else
                {
                    ares = (ListenerAsyncResult)_waitQueue[0];
                    _waitQueue.RemoveAt(0);
                }
            }
            ares?.Complete(context);
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock (_registry)
                _registry.Remove(context);
            lock (_ctxQueue)
            {
                var idx = _ctxQueue.IndexOf(context);
                if (idx >= 0)
                    _ctxQueue.RemoveAt(idx);
            }
        }

        internal void AddConnection(HttpConnection cnc)
        {
            _connections[cnc] = cnc;
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            _connections.Remove(cnc);
        }
    }
}
#endif