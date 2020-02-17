using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Implements a collection of components that automatically disposes each component
    /// implementing <see cref="IDisposable"/>.</para>
    /// <para>Each component in the collection may be given a unique name for later retrieval.</para>
    /// </summary>
    /// <typeparam name="T">The type of components in the collection.</typeparam>
    /// <seealso cref="ComponentCollection{T}" />
    /// <seealso cref="IComponentCollection{T}" />
    public class DisposableComponentCollection<T> : ComponentCollection<T>, IDisposable
    {
        /// <summary>
        /// Finalizes an instance of the <see cref="DisposableComponentCollection{T}"/> class.
        /// </summary>
        ~DisposableComponentCollection()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="true"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            foreach (var component in this)
            {
                if (component is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}