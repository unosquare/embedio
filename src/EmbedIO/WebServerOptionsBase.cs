using System;
using Swan.Configuration;

namespace EmbedIO
{
    /// <summary>
    /// Base class for web server options.
    /// </summary>
    public abstract class WebServerOptionsBase : ConfiguredObject
    {
        private bool _supportCompressedRequests;

        /// <summary>
        /// <para>Gets or sets a value indicating whether compressed request bodies are supported.</para>
        /// <para>The default value is <see langword="false"/>, because of the security risk
        /// posed by <see href="https://en.wikipedia.org/wiki/Zip_bomb">decompression bombs</see>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set and this instance's
        /// configuration is locked.</exception>
        public bool SupportCompressedRequests
        {
            get => _supportCompressedRequests;
            set
            {
                EnsureConfigurationNotLocked();
                _supportCompressedRequests = value;
            }
        }

        /// <summary>
        /// Locks this instance, preventing further configuration.
        /// </summary>
        public void Lock() => LockConfiguration();
    }
}