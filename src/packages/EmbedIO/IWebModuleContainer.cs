using System.Collections.Concurrent;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Represents an object that contains a collection of <see cref="IWebModule"/> interfaces.
    /// </summary>
    public interface IWebModuleContainer
    {
        /// <summary>
        /// Gets the modules.
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        IComponentCollection<IWebModule> Modules { get; }

        /// <summary>
        /// <para>Gets a dictionary of data shared among the modules in a container.</para>
        /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended to be used
        /// directly from your code, unless to address specific needs in module development.</para>
        /// </summary>
        ConcurrentDictionary<object, object> SharedItems { get; }
    }
}