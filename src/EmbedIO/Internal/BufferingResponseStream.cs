using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Internal
{
    // Wraps a response's output stream, buffering all data
    // in a MemoryStream.
    // When disposed, sets the response's ContentLength and copies all data
    // to the output stream.
    internal class BufferingResponseStream : Stream
    {
        private readonly IHttpResponse _response;
        private readonly MemoryStream _buffer;

        public BufferingResponseStream(IHttpResponse response)
        {
            _response = response;
            _buffer = new MemoryStream();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _buffer.Length;

        public override long Position
        {
            get => _buffer.Position;
            set => throw SeekingNotSupported();
        }
        
        public override void Flush() => _buffer.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _buffer.FlushAsync(cancellationToken);

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

        public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

        public override void WriteByte(byte value) => _buffer.WriteByte(value);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => _buffer.BeginWrite(buffer, offset, count, callback, state);

        public override void EndWrite(IAsyncResult asyncResult) => _buffer.EndWrite(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _buffer.WriteAsync(buffer, offset, count, cancellationToken);
        
        protected override void Dispose(bool disposing)
        {
            _response.ContentLength64 = _buffer.Length;
            _buffer.Position = 0;
            _buffer.CopyTo(_response.OutputStream);

            if (disposing)
            {
                _buffer.Dispose();
            }
        }

        private static Exception ReadingNotSupported() => new NotSupportedException("This stream does not support reading.");

        private static Exception SeekingNotSupported() => new NotSupportedException("This stream does not support seeking.");
    }
}