using System;
using System.Collections;
using System.Collections.Generic;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Implements a collection of components.</para>
    /// <para>Each component in the collection may be given a unique name for later retrieval.</para>
    /// </summary>
    /// <typeparam name="T">The type of components in the collection.</typeparam>
    /// <seealso cref="IComponentCollection{T}" />
    public class ComponentCollection<T> : IComponentCollection<T>
    {
        private List<T> _components = new List<T>();
        
        private List<(string, T)> _componentsWithSafeNames = new List<(string, T)>();

        private Dictionary<string, T> _namedComponents = new Dictionary<string, T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentCollection{T}"/> class.
        /// </summary>
        public ComponentCollection()
        {
        }

        /// <inheritdoc />
        public int Count => _components.Count;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, T> Named => _namedComponents;

        /// <inheritdoc />
        public IReadOnlyList<(string SafeName, T Component)> WithSafeNames => _componentsWithSafeNames;

        /// <summary>
        /// Gets a value indicating whether this collection is locked,
        /// preventing further additions.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if locked; otherwise, <see langword="false"/>.
        /// </value>
        /// <seealso cref="Locked"/>
        public bool Locked { get; private set; }

        /// <inheritdoc />
        public T this[int index] => _components[index];

        /// <inheritdoc />
        public T this[string key] => _namedComponents[key];

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _components.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_components).GetEnumerator();

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The collection is <see cref="Locked"/>.</exception>
        public void Add(string name, T component)
        {
            if (Locked)
                throw new InvalidOperationException("Cannot add a component to a locked collection.");

            if (name != null)
            {
                if (name.Length == 0)
                    throw new ArgumentException("Component name is empty.", nameof(name));

                if (_namedComponents.ContainsKey(name))
                    throw new ArgumentException("Duplicate component name.", nameof(name));
            }

            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (_components.Contains(component))
                throw new ArgumentException("Component has already been added.", nameof(component));

            _components.Add(component);
            _componentsWithSafeNames.Add((name ?? $"<{component.GetType().Name}>", component));
            if (name != null)
                _namedComponents.Add(name, component);
        }

        /// <summary>
        /// Locks the collection, preventing further additions.
        /// </summary>
        /// <seealso cref="Locked"/>
        public void Lock() => Locked = true;
    }
}