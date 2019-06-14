using System;
using System.Collections.Generic;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Manages a stack of MIME type providers.</para>
    /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
    /// </summary>
    /// <seealso cref="IMimeTypeProvider" />
    public sealed class MimeTypeProviderStack : IMimeTypeProvider
    {
        private readonly Stack<IMimeTypeProvider> _providers = new Stack<IMimeTypeProvider>();

        /// <summary>
        /// <para>Pushes the specified MIME type provider on the stack.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="provider">The <see cref="IMimeTypeProvider"/> interface to push on the stack.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/>is <see langword="null"/>.</exception>
        public void Push(IMimeTypeProvider provider)
            => _providers.Push(Validate.NotNull(nameof(provider), provider));

        /// <summary>
        /// <para>Removes the most recently added MIME type provider from the stack.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        public void Pop() => _providers.Pop();

        /// <inheritdoc />
        public string GetMimeType(string extension)
        {
            string result;
            foreach (var provider in _providers)
            {
                result = provider.GetMimeType(extension);
                if (result != null)
                    return result;
            }

            MimeTypes.Associations.TryGetValue(extension, out result);
            return result;
        }
    }
}