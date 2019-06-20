using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EmbedIO.Files;

namespace EmbedIO.Testing
{
    public sealed partial class MockFileProvider : IFileProvider
    {
        public const string RandomDataUrlPath = "/random.dat";

        private const string RandomDataPath = "random.dat";

        private readonly Random _random;
        private readonly MockFile _randomDataFile;
        private readonly MockDirectory _root;

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

        public event Action<string> ResourceChanged;

        public bool IsImmutable => false;

        public void Start(CancellationToken cancellationToken)
        {
        }

        public MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            if (string.IsNullOrEmpty(urlPath))
                return null;

            if (!urlPath.StartsWith("/"))
                return null;

            var path = urlPath.Substring(1);
            var (name, entry) = FindEntry(path);
            return GetResourceInfo(path, name, entry, mimeTypeProvider);
        }

        public Stream OpenFile(string path)
        {
            var (name, entry) = FindEntry(path);
            return entry is MockFile file ? new MemoryStream(file.Data, false) : null;
        }

        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
        {
            var (name, entry) = FindEntry(path);
            return entry is MockDirectory directory
                ? directory.Select(pair => GetResourceInfo(AppendNameToPath(path, name), name, entry, mimeTypeProvider))
                : Enumerable.Empty<MappedResourceInfo>();
        }

        public int GetRandomDataLength() => _randomDataFile.Data.Length;

        public byte[] GetRandomData() => _randomDataFile.Data;

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

        private MappedResourceInfo GetResourceInfo(string path, string name, MockDirectoryEntry entry, IMimeTypeProvider mimeTypeProvider)
        {
            switch (entry)
            {
                case MockFile file:
                    return MappedResourceInfo.ForFile(
                        path,
                        name,
                        file.LastModifiedUtc,
                        file.Data.Length,
                        mimeTypeProvider.GetMimeType(Path.GetExtension(name)));
                case MockDirectory directory:
                    return MappedResourceInfo.ForDirectory(string.Empty, name, _root.LastModifiedUtc);
                default:
                    return null;
            }
        }
        
        private static string AppendNameToPath(string path, string name)
            => string.IsNullOrEmpty(path) ? name : $"{path}/{name}";
    }
}