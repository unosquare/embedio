using System;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IWebModule"/>.
    /// </summary>
    public static partial class WebModuleExtensions
    {
        /// <summary>
        /// <para>Gets the underlying <see cref="IWebModuleImpl"/> interface of an <see cref="IWebModule"/>.</para>
        /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended to be used directly from your code,
        /// unless to fulfill very specific needs in the development of plug-ins (modules, etc.) for EmbedIO.</para>
        /// </summary>
        /// <param name="this">The <see cref="IWebModule"/> interface on which this method is called.</param>
        /// <returns>The underlying <see cref="IWebModuleImpl"/> interface representing
        /// the web module implementation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="this"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InternalErrorException">
        /// <paramref name="this"/> does not implement <see cref="IWebModuleImpl"/>.
        /// </exception>
        public static IWebModuleImpl GetImplementation(this IWebModule @this)
            => Validate.NotNull(nameof(@this), @this) as IWebModuleImpl
            ?? throw SelfCheck.Failure($"{@this.GetType().FullName} does not implement {nameof(IWebModuleImpl)}.");
    }
}