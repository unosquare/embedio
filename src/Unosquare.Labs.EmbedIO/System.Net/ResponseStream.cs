namespace Unosquare.Net
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class ResponseStream 
        : Stream
    {
        private static readonly byte[] Crlf = { 13, 10 };

        private readonly Stream _stream;
        private readonly HttpListenerResponse _response;
        private readonly bool _ignoreErrors;
        private bool _disposed;
        private bool _trailerSent;

        internal ResponseStream(Stream stream, HttpListenerResponse response, bool ignoreErrors)
        {
            _response = response;
            _ignoreErrors = ignoreErrors;
            _stream = stream;
        }

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

#if NET452 || NET47
        public override void Close()
#else
        public void Close()
#endif
        {
            if (_disposed) return;

            _disposed = true;
            var ms = GetHeaders();
            var chunked = _response.SendChunked;

            if (_stream.CanWrite)
            {
                try
                {
                    byte[] bytes;
                    if (ms != null)
                    {
                        var start = ms.Position;
                        if (chunked && !_trailerSent)
                        {
                            bytes = GetChunkSizeBytes(0, true);
                            ms.Position = ms.Length;
                            ms.Write(bytes, 0, bytes.Length);
                        }

                        InternalWrite(ms.ToArray(), (int) start, (int) (ms.Length - start));
                        _trailerSent = true;
                    }
                    else if (chunked && !_trailerSent)
                    {
                        bytes = GetChunkSizeBytes(0, true);
                        InternalWrite(bytes, 0, bytes.Length);
                        _trailerSent = true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignored
                }
                catch (IOException)
                {
                    // Ignore error due to connection reset by peer
                }
            }

            _response.Close();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            var chunked = _response.SendChunked;

            using (var ms = GetHeaders(false))
            {
                byte[] bytes;

                if (ms != null)
                {
                    var start = ms.Position; // After the possible preamble for the encoding
                    ms.Position = ms.Length;
                    if (chunked)
                    {
                        bytes = GetChunkSizeBytes(count, false);
                        ms.Write(bytes, 0, bytes.Length);
                    }

                    var newCount = Math.Min(count, 16384 - (int) ms.Position + (int) start);
                    ms.Write(buffer, offset, newCount);
                    count -= newCount;
                    offset += newCount;
                    InternalWrite(ms.ToArray(), (int) start, (int) (ms.Length - start));
                }
                else if (chunked)
                {
                    bytes = GetChunkSizeBytes(count, false);
                    InternalWrite(bytes, 0, bytes.Length);
                }
            }

            if (count > 0)
                InternalWrite(buffer, offset, count);

            if (chunked)
                InternalWrite(Crlf, 0, 2);
        }

        /// <inheritdoc />
        public override int Read([In, Out] byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        internal void InternalWrite(byte[] buffer, int offset, int count)
        {
            if (_ignoreErrors)
            {
                try
                {
                    _stream.Write(buffer, offset, count);
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                _stream.Write(buffer, offset, count);
            }
        }

        private static byte[] GetChunkSizeBytes(int size, bool final) => Encoding.UTF8.GetBytes($"{size:x}\r\n{(final ? "\r\n" : string.Empty)}");

        private MemoryStream GetHeaders(bool closing = true)
        {
            lock (_response.HeadersLock)
                return _response.HeadersSent ? null : _response.SendHeaders(closing);
        }
    }
}