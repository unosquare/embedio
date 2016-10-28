#if !NET452
//
// System.Net.ResponseStream
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
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
//

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net
{
    /// <summary>
    /// FIXME: Does this buffer the response until Close?
    /// Update: we send a single packet for the first non-chunked Write
    /// What happens when we set content-length to X and write X-1 bytes then close?
    /// what if we don't set content-length at all?
    /// </summary>
    /// <seealso cref="System.IO.Stream" />
    public class ResponseStream : Stream
    {
        readonly HttpListenerResponse _response;
        readonly bool _ignoreErrors;
        bool _disposed;
        bool _trailerSent;
        readonly Stream _stream;

        internal ResponseStream(Stream stream, HttpListenerResponse response, bool ignoreErrors)
        {
            _response = response;
            _ignoreErrors = ignoreErrors;
            _stream = stream;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <exception cref="System.NotSupportedException">
        /// </exception>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            if (_disposed == false)
            {
                _disposed = true;
                var ms = GetHeaders(true);
                var chunked = _response.SendChunked;
                if (_stream.CanWrite)
                {
                    try
                    {
                        byte[] bytes = null;
                        if (ms != null)
                        {
                            var start = ms.Position;
                            if (chunked && !_trailerSent)
                            {
                                bytes = GetChunkSizeBytes(0, true);
                                ms.Position = ms.Length;
                                ms.Write(bytes, 0, bytes.Length);
                            }
                            InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
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
        }

        MemoryStream GetHeaders(bool closing)
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

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
        }

        static readonly byte[] _crlf = { 13, 10 };
        static byte[] GetChunkSizeBytes(int size, bool final)
        {
            var str = string.Format("{0:x}\r\n{1}", size, final ? "\r\n" : "");
            return Encoding.GetEncoding(0).GetBytes(str);
        }

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

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            byte[] bytes = null;
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
                InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
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
                InternalWrite(_crlf, 0, 2);
        }

        //public IAsyncResult BeginWrite(byte[] buffer, int offset, int count,
        //                    AsyncCallback cback, object state)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(GetType().ToString());

        //    byte[] bytes = null;
        //    MemoryStream ms = GetHeaders(false);
        //    bool chunked = response.SendChunked;
        //    if (ms != null)
        //    {
        //        long start = ms.Position;
        //        ms.Position = ms.Length;
        //        if (chunked)
        //        {
        //            bytes = GetChunkSizeBytes(count, false);
        //            ms.Write(bytes, 0, bytes.Length);
        //        }
        //        ms.Write(buffer, offset, count);
        //        buffer = ms.GetBuffer();
        //        offset = (int)start;
        //        count = (int)(ms.Position - start);
        //    }
        //    else if (chunked)
        //    {
        //        bytes = GetChunkSizeBytes(count, false);
        //        InternalWrite(bytes, 0, bytes.Length);
        //    }

        //    return stream.BeginWrite(buffer, offset, count, cback, state);
        //}

        //public void EndWrite(IAsyncResult ares)
        //{
        //    if (disposed)
        //        throw new ObjectDisposedException(GetType().ToString());

        //    if (ignore_errors)
        //    {
        //        try
        //        {
        //            stream.EndWrite(ares);
        //            if (response.SendChunked)
        //                stream.Write(crlf, 0, 2);
        //        }
        //        catch { }
        //    }
        //    else
        //    {
        //        stream.EndWrite(ares);
        //        if (response.SendChunked)
        //            stream.Write(crlf, 0, 2);
        //    }
        //}

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
#endif