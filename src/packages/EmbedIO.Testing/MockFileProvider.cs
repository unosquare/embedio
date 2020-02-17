﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EmbedIO.Files;

namespace EmbedIO.Testing
{
    /// <summary>
    /// <para>Provides an <see cref="IFileProvider"/> interface
    /// that does not interfere with the file system.</para>
    /// <para>This class simulates a small file system
    /// with a root directory, a subdirectory, HTML index files,
    /// and a data file filled with random bytes.</para>
    /// </summary>
    /// <seealso cref="IFileProvider" />
    public sealed partial class MockFileProvider : IFileProvider
    {
        /// <summary>
        /// The file name of HTML indexes.
        /// </summary>
        public const string IndexFileName = "index.html";

        /// <summary>
        /// The URL path to the HTML index of the root directory.
        /// </summary>
        public const string IndexUrlPath = "/index.html";

        /// <summary>
        /// The name of the subdirectory.
        /// </summary>
        public const string SubDirectoryName = "sub";

        /// <summary>
        /// The URL path to the subdirectory.
        /// </summary>
        public const string SubDirectoryUrlPath = "/sub";

        /// <summary>
        /// The URL path to the subdirectory HTML index.
        /// </summary>
        public const string SubDirectoryIndexUrlPath = "/sub/index.html";

        /// <summary>
        /// The URL path to a file containing random data.
        /// </summary>
        /// <seealso cref="GetRandomDataLength"/>
        /// <seealso cref="GetRandomData"/>
        /// <seealso cref="ChangeRandomData"/>
        public const string RandomDataUrlPath = "/random.dat";

        private const string RandomDataPath = "random.dat";

        private readonly Random _random;
        private readonly MockFile _randomDataFile;
        private readonly MockDirectory _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockFileProvider"/> class.
        /// </summary>
        public MockFileProvider()
        {
            _random = new Random();
            _randomDataFile = new MockFile(CreateRandomData(10000));
            _root = new MockDirectory {
                { "index.html", StockResource.GetBytes("index.html") },
                { "random.dat",  _randomDataFile },
                { "sub", new MockDirectory {
                    { "index.html", StockResource.GetBytes("sub.index.html") },
                } },
            };

        }

        /// <inheritdoc />
        public event Action<string> ResourceChanged;

        /// <inheritdoc />
        public bool IsImmutable => false;

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
        }

        /// <inheritdoc />
        public MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            if (string.IsNullOrEmpty(urlPath))
                return null;

            if (!urlPath.StartsWith("/"))
                return null;

            var path = urlPath.Substring(1);
            var (name, entry) = FindEntry(path);
            return GetResourceInfo(path, name, entry, mimeTypeProvider);
        }

        /// <inheritdoc />
        public Stream? OpenFile(string path)
        {
            var (_, entry) = FindEntry(path);
            return entry is MockFile file ? new MemoryStream(file.Data, false) : null;
        }

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
        {
            var (name, entry) = FindEntry(path);
            return entry is MockDirectory directory
                ? directory.Select(pair => GetResourceInfo(AppendNameToPath(path, name), name, entry, mimeTypeProvider))
                : Enumerable.Empty<MappedResourceInfo>();
        }

        /// <summary>
        /// Gets the length of the random data file,
        /// so it can be compared to the length of returned content.
        /// </summary>
        /// <returns>The length of the random data file.</returns>
        /// <seealso cref="RandomDataUrlPath"/>
        /// <seealso cref="GetRandomData"/>
        /// <seealso cref="ChangeRandomData"/>
        public int GetRandomDataLength() => _randomDataFile.Data.Length;

        /// <summary>
        /// Gets the same random data that should be returned
        /// in response to a request for the random data file.
        /// </summary>
        /// <returns>An array of bytes containing random data.</returns>
        /// <seealso cref="GetRandomDataLength"/>
        /// <seealso cref="GetRandomData"/>
        /// <seealso cref="ChangeRandomData"/>
        public byte[] GetRandomData() => _randomDataFile.Data;

        /// <summary>
        /// <para>Creates and returns a new set of random data bytes.</para>
        /// <para>After this method returns, requests for the random data file
        /// should return the same bytes returned by this method.</para>
        /// </summary>
        /// <param name="newLength">The length of the new random data.</param>
        /// <returns>An array of bytes containing the new random data.</returns>
        public byte[] ChangeRandomData(int newLength)
        {
            var data = CreateRandomData(newLength);
            _randomDataFile.SetData(data);
            ResourceChanged?.Invoke(RandomDataPath);
            return data;
        }

        private byte[] CreateRandomData(int length)
        {
            var result = new byte[length];
            _random.NextBytes(result);
            return result;
        }

        private (string name, MockDirectoryEntry entry) FindEntry(string path)
        {
            if (path == null)
                return default;

            if (path.Length == 0)
                return (string.Empty, _root);

            var dir = _root;
            var segments = path.Split('/');
            var lastIndex = segments.Length - 1;
            var i = 0;
            foreach (var segment in segments)
            {
                if (!dir.TryGetValue(segment, out var entry))
                    return default;

                if (i == lastIndex && entry is MockFile file)
                    return (segment, file);

                if (!(entry is MockDirectory directory))
                    return default;

                if (i == lastIndex)
                    return (segment, directory);

                dir = directory;
                i++;
            }

            return default;
        }

        private MappedResourceInfo? GetResourceInfo(string path, string name, MockDirectoryEntry entry, IMimeTypeProvider mimeTypeProvider) => entry switch {
            MockFile file => MappedResourceInfo.ForFile(path, name, file.LastModifiedUtc, file.Data.Length, mimeTypeProvider.GetMimeType(Path.GetExtension(name))),
            MockDirectory _ => MappedResourceInfo.ForDirectory(string.Empty, name, _root.LastModifiedUtc),
            _ => null
        };

        private static string AppendNameToPath(string path, string name)
            => string.IsNullOrEmpty(path) ? name : $"{path}/{name}";
    }
}