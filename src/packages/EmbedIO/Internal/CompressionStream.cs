using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Internal
{
    internal class CompressionStream : Stream
    {
        private readonly Stream _target;
        private readonly bool _leaveOpen;

        public CompressionStream(Stream target, CompressionMethod compressionMethod)
        {
            switch (compressionMethod)
            {
                case CompressionMethod.Deflate:
                    _target = new DeflateStream(target, CompressionMode.Compress, true);
                    _leaveOpen = false;
                    break;
                case CompressionMethod.Gzip:
                    _target = new GZipStream(target, CompressionMode.Compress, true);
                    _leaveOpen = false;
                    break;
                default:
                    _target = target;
                    _leaveOpen = true;
                    break;
            }

            UncompressedLength = 0;
        }

        public long UncompressedLength { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw SeekingNotSupported();
        
        public override long Position
        {
            get => throw SeekingNotSupported();
            set => throw SeekingNotSupported();
        }

        public override void Flush() => _target.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _target.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw ReadingNotSupported();

        public override int ReadByte() => throw ReadingNotSupported();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => throw ReadingNotSupported();

        public override int EndRead(IAsyncResult asyncResult) => throw ReadingNotSupported();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw ReadingNotSupported();

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => throw ReadingNotSupported();

        public override long Seek(long offset, SeekOrigin origin) => throw SeekingNotSupported();

        public override void SetLength(long value) => throw SeekingNotSupported();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _target.Write(buffer, offset, count);
            UncompressedLength += count;
        }

        public override void WriteByte(byte value)
        {
            _target.WriteByte(value);
            UncompressedLength++;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => _target.BeginWrite(
                buffer,
                offset,
                count,
                ar => {
                    UncompressedLength += count;
                    callback(ar);
                },
                state);

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _target.EndWrite(asyncResult);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _target.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            UncompressedLength += count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
            {
                _target.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Exception ReadingNotSupported() => new NotSupportedException("This stream does not support reading.");

        private static Exception SeekingNotSupported() => new NotSupportedException("This stream does not support seeking.");
    }
}