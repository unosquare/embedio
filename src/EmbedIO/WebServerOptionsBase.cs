using System;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Base class for web server options.
    /// </summary>
    public abstract class WebServerOptionsBase : ConfiguredObject
    {
        private bool _supportCompressedRequests;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerOptionsBase" /> class.
        /// </summary>
        protected WebServerOptionsBase()
        {
        }

        /// <summary>
        /// Locks this instance, preventing further configuration.
        /// </summary>
        public void Lock() => LockConfiguration();
    }
}