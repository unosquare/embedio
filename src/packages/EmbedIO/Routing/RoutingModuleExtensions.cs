using System;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Provides extension methods for <see cref="RoutingModule"/>.
    /// </summary>
    public static partial class RoutingModuleExtensions
    {
        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteAttribute">Route</see> attributes.</para>
        /// <para>See <see cref="RouteVerbResolverCollection.AddFrom(object)"/> for further information.</para>
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="target">Where to look for compatible handlers.</param>
        /// <returns><paramref name="this"/> with handlers added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        public static RoutingModule WithHandlersFrom(this RoutingModule @this, object target)
        {
            @this.AddFrom(target);
            return @this;
        }
    }
}