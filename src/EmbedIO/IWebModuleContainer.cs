using System;
using Swan.Abstractions;

namespace EmbedIO
{
    /// <summary>
    /// Represents an object that contains a collection of <see cref="IWebModule"/> interfaces.
    /// </summary>
    public interface IWebModuleContainer : IDisposable
    {
        /// <summary>
        /// Gets the modules.
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        IComponentCollection<IWebModule> Modules { get; }
    }
}