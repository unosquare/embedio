#if !NET47
//
// System.Net.ResponseStream
//
// Author:
// Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
namespace Unosquare.Net
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Represents a Response stream.
    /// </summary>
    /// <seealso cref="System.IO.Stream" />
    public class ResponseStream 
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

        /// <summary>
        /// Closes this instance.
        /// </summary>
#if NET46
        public override void Close()
#else
        public void Close()
#endif
        {
            if (_disposed) return;

            _disposed = true;
            var ms = GetHeaders(true);
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

                        InternalWrite(ms.ToArray(), (int)start, (int)(ms.Length - start));
                        _trailerSent = true;
                    }
                    else if (chunked && !_trailerSent)
                    {
                        bytes = GetChunkSizeBytes(0, true);
                        InternalWrite(bytes, 0, bytes.Length);
                        _trailerSent = true;
                    }
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

            byte[] bytes;
            var ms = GetHeaders(false);
            var chunked = _response.SendChunked;
            if (ms != null)
            {
                var start = ms.Position; // After the possible preamble for the encoding
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(count, false);
                    ms.Write(bytes, 0, bytes.Length);
                }

                var newCount = Math.Min(count, 16384 - (int)ms.Position + (int)start);
                ms.Write(buffer, offset, newCount);
                count -= newCount;
                offset += newCount;
                InternalWrite(ms.ToArray(), (int)start, (int)(ms.Length - start));
                ms.SetLength(0);
                ms.Capacity = 0; // 'dispose' the buffer in ms.
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(count, false);
                InternalWrite(bytes, 0, bytes.Length);
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

        private MemoryStream GetHeaders(bool closing)
        {
            // SendHeaders works on shared headers
            lock (_response.HeadersLock)
            {
                if (_response.HeadersSent)
                    return null;

                var ms = new MemoryStream();
                _response.SendHeaders(closing, ms);
                return ms;
            }
        }
    }
}
#endif