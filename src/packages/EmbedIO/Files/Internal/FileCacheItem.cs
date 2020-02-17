using System;
using EmbedIO.Internal;

namespace EmbedIO.Files.Internal
{
    internal sealed class FileCacheItem
    {
#pragma warning disable SA1401 // Field should be private - performance is a stronger concern here.
        // These fields create a sort of linked list of items
        // inside the cache's dictionary.
        // Their purpose is to keep track of items
        // in order from least to most recently used.
        internal string? PreviousKey;
        internal string? NextKey;
        internal long LastUsedAt;
#pragma warning restore SA1401

        // Size of a pointer in bytes
        private static readonly long SizeOfPointer = Environment.Is64BitProcess ? 8 : 4;

        // Size of a WeakReference<T> in bytes
        private static readonly long SizeOfWeakReference = Environment.Is64BitProcess ? 16 : 32;

        private readonly object _syncRoot = new object();

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
        private byte[]? _uncompressedContent;
        private byte[]? _gzippedContent;
        private byte[]? _deflatedContent;

        internal FileCacheItem(FileCache.Section section, DateTime lastModifiedUtc, long length)
        {
            _section = new WeakReference<FileCache.Section>(section);

            LastModifiedUtc = lastModifiedUtc;
            Length = length;

            // There is no way to know the actual size of an object at runtime.
            // This method makes some educated guesses, based on the following
            // article (among others):
            // https://codingsight.com/precise-computation-of-clr-object-size/
            // PreviousKey and NextKey values aren't counted in
            // because they are just references to existing strings.
            SizeInCache = SizeOfItem + SizeOfWeakReference;
        }

        public DateTime LastModifiedUtc { get; }

        public long Length { get; }

        // This is the (approximate) in-memory size of this object.
        // It is NOT the length of the cache resource!
        public long SizeInCache { get; private set; }

        public byte[]? GetContent(CompressionMethod compressionMethod)
        {
            // If there are both entity tag and content, use them.
            switch (compressionMethod)
            {
                case CompressionMethod.Deflate:
                    if (_deflatedContent != null) return _deflatedContent;
                    break;
                case CompressionMethod.Gzip:
                    if (_gzippedContent != null) return _gzippedContent;
                    break;
                default:
                    if (_uncompressedContent != null) return _uncompressedContent;
                    break;
            }

            // Try to convert existing content, if any.
            byte[]? content;
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
                // No content whatsoever.
                return null;
            }

            return SetContent(compressionMethod, content);
        }

        public byte[]? SetContent(CompressionMethod compressionMethod, byte[]? content)
        {
            // This is the bare minimum locking we need
            // to ensure we don't mess sizes up.
            byte[]? oldContent;
            lock (_syncRoot)
            {
                switch (compressionMethod)
                {
                    case CompressionMethod.Deflate:
                        oldContent = _deflatedContent;
                        _deflatedContent = content;
                        break;
                    case CompressionMethod.Gzip:
                        oldContent = _gzippedContent;
                        _gzippedContent = content;
                        break;
                    default:
                        oldContent = _uncompressedContent;
                        _uncompressedContent = content;
                        break;
                }
            }

            var sizeDelta = GetSizeOf(content) - GetSizeOf(oldContent);
            SizeInCache += sizeDelta;
            if (_section.TryGetTarget(out var section))
                section.UpdateTotalSize(sizeDelta);

            return content;
        }

        // Round up to a multiple of 16
        private static long RoundUpTo16(long n)
        {
            var remainder = n % 16;
            return remainder > 0 ? n + (16 - remainder) : n;
        }

        // The size of a byte array is 3 * SizeOfPointer + 1 (size of byte) * Length
        private static long GetSizeOf(byte[]? arr) => arr == null ? 0 : RoundUpTo16(3 * SizeOfPointer) + arr.Length;
    }
}