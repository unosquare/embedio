#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a wrapper for Microsoft HTTP Listener.
    /// </summary>
    internal class HttpListener : IHttpListener
    {
        private readonly System.Net.HttpListener _httpListener;
        
        public HttpListener(System.Net.HttpListener httpListener)
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
        public List<string> Prefixes => _httpListener.Prefixes.Select(y => y.ToString()).ToList();

        /// <inheritdoc />
        public bool IsListening => _httpListener.IsListening;
        
        /// <inheritdoc />
        public void Start() => _httpListener.Start();

        /// <inheritdoc />
        public void Stop() => _httpListener.Stop();

        /// <inheritdoc />
        public void AddPrefix(string urlPrefix) => _httpListener.Prefixes.Add(urlPrefix);

        /// <inheritdoc />
        public async Task<IHttpContext> GetContextAsync()=> new HttpContext(await _httpListener.GetContextAsync());

        void IDisposable.Dispose()
        {
            ((IDisposable) _httpListener)?.Dispose();
        }
    }
}
#endif