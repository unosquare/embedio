using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EmbedIO.Net.Internal
{
    internal class RequestStream : Stream
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private int _offset;
        private int _length;
        private long _remainingBody;

        internal RequestStream(Stream stream, byte[] buffer, int offset, int length, long contentLength = -1)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _length = length;
            _remainingBody = contentLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            var nread = FillFromBuffer(buffer, offset, count);

            if (nread == -1)
            {
                // No more bytes available (Content-Length)
                return 0;
            }

            if (nread > 0)
            {
                return nread;
            }

            nread = _stream.Read(buffer, offset, count);

            if (nread > 0 && _remainingBody > 0)
                _remainingBody -= nread;

            return nread;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        
        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        private int FillFromBuffer(byte[] buffer, int off, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (off < 0)
                throw new ArgumentOutOfRangeException(nameof(off), "< 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "< 0");

            var len = buffer.Length;

            if (off > len)
                throw new ArgumentException("destination offset is beyond array size");
            if (off > len - count)
                throw new ArgumentException("Reading would overrun buffer");

            if (_remainingBody == 0)
                return -1;

            if (_length == 0)
                return 0;

            var size = Math.Min(_length, count);
            if (_remainingBody > 0)
                size = (int) Math.Min(size, _remainingBody);

            if (_offset > _buffer.Length - size)
            {
                size = Math.Min(size, _buffer.Length - _offset);
            }

            if (size == 0)
                return 0;

            Buffer.BlockCopy(_buffer, _offset, buffer, off, size);
            _offset += size;
            _length -= size;
            if (_remainingBody > 0)
                _remainingBody -= size;
            return size;
        }
    }
}