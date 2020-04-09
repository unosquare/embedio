using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EmbedIO.Utilities;

namespace EmbedIO.Testing
{
    /// <summary>
    /// <para>Provides access to standard resources embedded in <c>EmbedIO.Testing.dll</c>.</para>
    /// <para>Resources are organized in folders; access to a resource happens in a way
    /// similar to URL paths, i.e. using slashes (<c>/</c>) as separators.</para>
    /// </summary>
    public static class StockResource
    {
        private static readonly string Prefix = typeof(TestWebServer).Namespace + ".Resources.";

        private static readonly Assembly Assembly;

        static StockResource()
        {
            Assembly = Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Gets an enumeration of paths to all the defined stock resources.
        /// </summary>
        // NOTES TO CONTRIBUTORS:
        // =====================
        // 1. Be careful to keep this array in sync with actual embedded resources.
        // 2. There is currently no way to determine paths at runtime,
        //    because the distinction between slashes and dots gets lost
        //    when using Assembly.GetManifestResourceNames.
        // 3. The property type is IEnumerable<string>, so
        //    enumerating resources dynamically will not be a breaking change
        //    if someone finds a way to do it.
        public static IEnumerable<string> Paths { get; } = new[] {
            "/index.html",
            "/sub/index.html",
        };

        /// <summary>
        /// Determines whether a stock resource exists.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns><see langword="true"/> if the resource exists;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool Exists(string path)
            => Assembly.GetManifestResourceNames().Contains(ConvertPath(path));

        /// <summary>
        /// Attempts to load a resource.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="stream">When this method returns <see langword="true"/>,
        /// a <see cref="Stream"/> representing the resource.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified resource
        /// has been loaded; otherwise, <see langword="false"/>.</returns>
        public static bool TryOpen(string path, out Stream? stream)
        {
            stream = null;
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                stream = Assembly.GetManifestResourceStream(ConvertPath(path));
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads the specified resource.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns>A <see cref="Stream"/> representing the resource,
        /// or <see langword="null"/> if the resource is not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is an empty string.</exception>
        public static Stream Open(string path)
            => Assembly.GetManifestResourceStream(ConvertPath(Validate.NotNullOrEmpty(nameof(path), path)));

        /// <summary>
        /// Gets the length of a resource, expressed in bytes.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns>The length of the specified resource.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is an empty string.</exception>
        public static long GetLength(string path)
        {
            using var stream = Open(path);
            return stream.Length;
        }

        /// <summary>
        /// Gets a resource as an array of bytes.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns>An array of bytes containing the resource's contents.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is an empty string.</exception>
        public static byte[] GetBytes(string path)
        {
            using var stream = Open(path);
            var length = (int)stream.Length;
            if (length == 0)
                return Array.Empty<byte>();

            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return buffer;
        }

        /// <summary>
        /// <para>Gets a range of bytes from a resource's contents.</para>
        /// <para>The range must be specified the same way as in HTTP <c>Range</c> headers,
        /// i.e. with a starting offset and an inclusive upper bound; for example,
        /// if <paramref name="start"/>is 200 and <paramref name="upperBound"/> is 299
        /// then 100 bytes are returned, starting from the 201st byte (as indexes are 0-based).</para>
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="start">The starting offset of the range to return.</param>
        /// <param name="upperBound">The inclusive upper bound of the range to return.</param>
        /// <returns>An array of bytes containing the specified range of the resource's contents,
        /// or <see langword="null"/> if the range is not valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is an empty string.</exception>
        public static byte[]? GetByteRange(string path, int start, int upperBound)
        {
            using var stream = Open(path);
            var length = (int) stream.Length;
            if (start >= length || upperBound < start || upperBound >= length)
                return null;

            var rangeLength = upperBound - start + 1;
            var buffer = new byte[rangeLength];
            stream.Position = start;
            stream.Read(buffer, 0, rangeLength);
            return buffer;
        }

        /// <summary>
        /// Gets a resource as text.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="encoding">The encoding to use to convert the resource's content
        /// to a string. If <see langword="null"/> is specified (the default),
        /// UTF-8 will be used.</param>
        /// <returns>The specified resource as a <see langword="string"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is an empty string.</exception>
        public static string GetText(string path, Encoding? encoding = null)
        {
            using var stream = Open(path);
            using var reader = new StreamReader(stream, encoding ?? WebServer.DefaultEncoding, false, WebServer.StreamCopyBufferSize, true);
            return reader.ReadToEnd();
        }

        private static string? ConvertPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path[0] == '/')
                path = path.Substring(1);

            return Prefix + path.Replace('/', '.');
        }
    }
}