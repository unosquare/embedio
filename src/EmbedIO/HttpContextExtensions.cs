using System;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpContext"/>.
    /// </summary>
    public static partial class HttpContextExtensions
    {
        /// <summary>
        /// <para>Gets the underlying <see cref="IHttpContextImpl"/> interface of an <see cref="IHttpContext"/>.</para>
        /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended to be used directly from your code,
        /// unless to fulfill very specific needs in the development of plug-ins (modules, etc.) for EmbedIO.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <returns>The underlying <see cref="IHttpContextImpl"/> interface representing
        /// the HTTP context implementation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="this"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="EmbedIOInternalErrorException">
        /// <paramref name="this"/> does not implement <see cref="IHttpContextImpl"/>.
        /// </exception>
        public static IHttpContextImpl GetImplementation(this IHttpContext @this)
            => Validate.NotNull(nameof(@this), @this) as IHttpContextImpl
            ?? throw SelfCheck.Failure($"{@this.GetType().FullName} does not implement {nameof(IHttpContextImpl)}.");
    }
}