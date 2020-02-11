using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IComponentCollection{T}"/>.
    /// </summary>
    public static class ComponentCollectionExtensions
    {
        /// <summary>
        /// Adds the specified component to a collection, without giving it a name.
        /// </summary>
        /// <typeparam name="T">The type of components in the collection.</typeparam>
        /// <param name="this">The <see cref="IComponentCollection{T}" /> on which this method is called.</param>
        /// <param name="component">The component to add.</param>
        /// <exception cref="NullReferenceException"><paramref name="this" /> is <see langword="null" />.</exception>
        /// <seealso cref="IComponentCollection{T}.Add" />
        public static void Add<T>(this IComponentCollection<T> @this, T component) => @this.Add(null, component);
    }
}