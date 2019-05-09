using System;

namespace EmbedIO.Modules
{
    public static class FileModuleBaseExtensions
    {
        /// <summary>
        /// Adds a default header to a <see cref="FileModuleBase"/>-derived module.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns><paramref name="this"/>with the default header added.</returns>
        /// <exception cref="ArgumentNullException">this</exception>
        public static TModule WithDefaultHeader<TModule>(this TModule @this, string headerName, string value)
            where TModule : FileModuleBase
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.DefaultHeaders.Add(headerName, value);
            return @this;
        }
    }
}