using System;
using EmbedIO.Internal;

namespace EmbedIO.Files.Internal
{
    internal sealed class FileCacheItem
    {
#pragma warning disable SA1401 // Field should be private - performance is a strongest concern here.
        // These fields create a sort of linked list of items
        // inside the cache's dictionary.
        // Their purpose is to keep track of items
        // in order from least to most recently used.
        internal string PreviousKey;
        internal string NextKey;
        internal long LastUsedAt;
#pragma warning restore SA1401

        // Size of a pointer in bytes
        private static readonly long SizeOfPointer = Environment.Is64BitProcess ? 8 : 4;

        // Size of a WeakReference<T> in bytes
        private static readonly long SizeOfWeakReference = Environment.Is64BitProcess ? 16 : 32;

        // Educated guess about the size of an Item in memory (see comments on constructor).
        // 3 * SizeOfPointer + total size of fields, rounded up to a multiple of 16.
        //
        // Computed as follows:
        //
        // * for 32-bit:
        //     - initialize count to 3 (number of "hidden" pointers that compose the object header)
        //     - for every field / auto property, in order of declaration:
        //         - increment count by 1 for reference types, 2 for long and DateTime
        //           (as of time of writing there are no fields of other types here)
        //         - increment again by 1 if this field "weighs" 1 and the next one "weighs" 2
        //           (padding for field alignment)
        //     - multiply count by 4 (size of a pointer)
        //     - if the result is not a multiple of 16, round it up to next multiple of 16
        //
        // * for 64-bit:
        //     - initialize count to 3 (number of "hidden" pointers that compose the object header)
        //     - for every field / auto property, in order of declaration, increment count by 1
        //       (at the time of writing there are no fields here that need padding on 64-bit)
        //     - multiply count by 8 (size of a pointer)
        //     - if the result is not a multiple of 16, round it up to next multiple of 16
        private static readonly long SizeOfItem = Environment.Is64BitProcess ? 96 : 128;

        // Used to update total size of section.
        // Weak reference avoids circularity.
        private readonly WeakReference<FileCache.Section> _section;

        // There are only 3 possible compression methods,
        // hence a dictionary (or two dictionaries) would be overkill.
        private byte[] _uncompressedContent;
        private byte[] _gzippedContent;
        private byte[] _deflatedContent;
        private string _uncompressedEntityTag;
        private string _gzippedEntityTag;
        private string _deflatedEntityTag;

        internal FileCacheItem(FileCache.Section section, DateTime lastModifiedUtc)
        {
            _section = new WeakReference<FileCache.Section>(section);

            LastModifiedUtc = lastModifiedUtc;

            // There is no way to know the actual size of an object at runtime.
            // This method makes some educated guesses, based on the following
            // article (among others):
            // https://codingsight.com/precise-computation-of-clr-object-size/
            // PreviousKey and NextKey values aren't counted in
            // because they are just references to existing strings.
            Size = SizeOfItem + SizeOfWeakReference;
        }

        public DateTime LastModifiedUtc { get; }

        public long Size { get; private set; }

        public (byte[], string) GetContentAndEntityTag(CompressionMethod compressionMethod)
        {
            // If there are both entity tag and content, use them.
            switch (compressionMethod)
            {
                case CompressionMethod.Deflate:
                    if (_deflatedContent != null) return (_deflatedContent, _deflatedEntityTag);
                    break;
                case CompressionMethod.Gzip:
                    if (_gzippedContent != null) return (_gzippedContent, _gzippedEntityTag);
                    break;
                default:
                    if (_uncompressedContent != null) return (_uncompressedContent, _uncompressedEntityTag);
                    break;
            }

            // Try to convert existing content, if any.
            byte[] content = null;
            if (_uncompressedContent != null)
            {
                content = CompressionUtility.ConvertCompression(_uncompressedContent, CompressionMethod.None, compressionMethod);
            }
            else if (_gzippedContent != null)
            {
                content = CompressionUtility.ConvertCompression(_gzippedContent, CompressionMethod.Gzip, compressionMethod);
            }
            else if (_deflatedContent != null)
            {
                content = CompressionUtility.ConvertCompression(_deflatedContent, CompressionMethod.Deflate, compressionMethod);
            }
            else
            {
                // No content whatsoever: try to return at least the entity tag if it's there.
                switch (compressionMethod)
                {
                    case CompressionMethod.Deflate:
                        return (null, _deflatedEntityTag);
                    case CompressionMethod.Gzip:
                        return (null, _gzippedEntityTag);
                    default:
                        return (null, _uncompressedEntityTag);
                }
            }

            var entityTag = EntityTag.Compute(LastModifiedUtc, content);
            return SetContentAndEntityTag(compressionMethod, content, entityTag);
        }

        public (byte[], string) SetContentAndEntityTag(CompressionMethod compressionMethod, byte[] content, string entityTag)
        {
            // Content can be null (when FileModule.ContentCaching = false).
            // Entity tag MUST NOT be null!
            SelfCheck.Assert(entityTag != null,
                $"Null {nameof(entityTag)} passed to {nameof(FileCacheItem)}.{nameof(SetContentAndEntityTag)}.");

            // This is the bare minimum locking we need
            // to ensure we don't mess sizes up.
            byte[] oldContent;
            string oldEntityTag;
            lock (this)
            {
                switch (compressionMethod)
                {
                    case CompressionMethod.Deflate:
                        oldContent = _deflatedContent;
                        oldEntityTag = _deflatedEntityTag;
                        _deflatedContent = content;
                        _deflatedEntityTag = entityTag;
                        break;
                    case CompressionMethod.Gzip:
                        oldContent = _gzippedContent;
                        oldEntityTag = _gzippedEntityTag;
                        _gzippedContent = content;
                        _gzippedEntityTag = entityTag;
                        break;
                    default:
                        oldContent = _uncompressedContent;
                        oldEntityTag = _uncompressedEntityTag;
                        _uncompressedContent = content;
                        _uncompressedEntityTag = entityTag;
                        break;
                }
            }

            var sizeDelta = GetSizeOf(content) + GetSizeOf(entityTag)
                          - GetSizeOf(oldContent) - GetSizeOf(oldEntityTag);
            Size += sizeDelta;
            if (_section.TryGetTarget(out var section))
                section.UpdateTotalSize(sizeDelta);

            return (content, entityTag);
        }

        // Round up to a multiple of 16
        private static long RoundUpTo16(long n)
        {
            var remainder = n % 16;
            return remainder > 0 ? n + (16 - remainder) : n;
        }

        // The size of a string is 3 * SizeOfPointer + 4 (size of Length field) + 2 (size of char) * Length
        // String has a m_firstChar field that always exists at the same address as its array of characters,
        // thus even the empty string is considered of length 1.
        private long GetSizeOf(string str) => str == null ? 0 : RoundUpTo16(3 * SizeOfPointer) + 4 + (2 * Math.Min(1, str.Length));

        // The size of a byte array is 3 * SizeOfPointer + 1 (size of byte) * Length
        private long GetSizeOf(byte[] arr) => arr == null ? 0 : RoundUpTo16(3 * SizeOfPointer) + arr.Length;
    }
}