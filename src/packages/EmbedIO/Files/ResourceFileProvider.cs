using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using EmbedIO.Utilities;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides access to embedded resources to a <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="IFileProvider" />
    public class ResourceFileProvider : IFileProvider
    {
        private readonly DateTime _fileTime = DateTime.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFileProvider"/> class.
        /// </summary>
        /// <param name="assembly">The assembly where served files are contained as embedded resources.</param>
        /// <param name="pathPrefix">A string to prepend to provider-specific paths
        /// to form the name of a manifest resource in <paramref name="assembly"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
        public ResourceFileProvider(Assembly assembly, string pathPrefix)
        {
            Assembly = Validate.NotNull(nameof(assembly), assembly);
            PathPrefix = pathPrefix ?? string.Empty;
        }

        /// <inheritdoc />
        public event Action<string>? ResourceChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Gets the assembly where served files are contained as embedded resources.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Gets a string that is prepended to provider-specific paths to form the name of a manifest resource in <see cref="Assembly"/>.
        /// </summary>
        public string PathPrefix { get; }

        /// <inheritdoc />
        public bool IsImmutable => true;

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
        }

        /// <inheritdoc />
        public MappedResourceInfo? MapUrlPath(string path, IMimeTypeProvider mimeTypeProvider)
        {
            var resourceName = PathPrefix + Validate.UrlPath(nameof(path), path, false).Replace('/', '.');
            Validate.NotNull(nameof(mimeTypeProvider), mimeTypeProvider);

            long size;
            try
            {
                using var stream = Assembly.GetManifestResourceStream(resourceName);
                if (stream == null || stream == Stream.Null)
                    return null;

                size = stream.Length;
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            var lastSlashPos = path.LastIndexOf('/');
            var name = path.Substring(lastSlashPos + 1);

            return MappedResourceInfo.ForFile(
                resourceName,
                name,
                _fileTime,
                size,
                mimeTypeProvider.GetMimeType(Path.GetExtension(name)));
        }

        /// <inheritdoc />
        public Stream OpenFile(string providerPath) => Assembly.GetManifestResourceStream(providerPath);

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string providerPath, IMimeTypeProvider mimeTypeProvider)
            => Enumerable.Empty<MappedResourceInfo>();
    }
}