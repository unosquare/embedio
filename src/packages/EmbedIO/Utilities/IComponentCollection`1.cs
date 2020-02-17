using System;
using System.Collections.Generic;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Represents a collection of components.</para>
    /// <para>Each component in the collection may be given a unique name for later retrieval.</para>
    /// </summary>
    /// <typeparam name="T">The type of components in the collection.</typeparam>
    public interface IComponentCollection<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// Gets an <see cref="IReadOnlyDictionary{TKey,TValue}"/> interface representing the named components.
        /// </summary>
        /// <value>
        /// The named components.
        /// </value>
        IReadOnlyDictionary<string, T> Named { get; }

        /// <summary>
        /// <para>Gets an <see cref="IReadOnlyList{T}"/> interface representing all components
        /// associated with safe names.</para>
        /// <para>The safe name of a component is never <see langword="null"/>.
        /// If a component's unique name if <see langword="null"/>, its safe name
        /// will be some non-<see langword="null"/> string somehow identifying it.</para>
        /// <para>Note that safe names are not necessarily unique.</para>
        /// </summary>
        /// <value>
        /// A list of <see cref="ValueTuple{T1,T2}"/>s, each containing a safe name and a component.
        /// </value>
        IReadOnlyList<(string SafeName, T Component)> WithSafeNames { get; }

        /// <summary>
        /// Gets the component with the specified name.
        /// </summary>
        /// <value>
        /// The component.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns>The component with the specified <paramref name="name"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="name"/> is not found.</exception>
        T this[string name] { get; }

        /// <summary>
        /// Adds a component to the collection,
        /// giving it the specified <paramref name="name"/> if it is not <see langword="null"/>.
        /// </summary>
        /// <param name="name">The name given to the module, or <see langword="null"/>.</param>
        /// <param name="component">The component.</param>
        void Add(string? name, T component);
    }
}