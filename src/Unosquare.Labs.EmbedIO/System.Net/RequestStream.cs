#if !NET46
//
// System.Net.RequestStream
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

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Unosquare.Net
{
    internal class RequestStream : Stream
    {
        readonly byte[] _buffer;
        int _offset;
        int _length;
        long _remainingBody;
        bool _disposed = false;
        readonly Stream _stream;
        
        internal RequestStream(Stream stream, byte[] buffer, int offset, int length, long contentlength = -1)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _length = length;
            _remainingBody = contentlength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        
        public override void Flush()
        {
        }
        
        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        int FillFromBuffer(byte[] buffer, int off, int count)
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
                size = (int)Math.Min(size, _remainingBody);

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

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            var nread = FillFromBuffer(buffer, offset, count);
            if (nread == -1)
            { // No more bytes available (Content-Length)
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

        #if CHUNKED
#if !NETSTANDARD1_6
        new 
#endif
        public IAsyncResult BeginRead(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            var nread = FillFromBuffer(buffer, offset, count);
            if (nread > 0 || nread == -1)
            {
                var ares = new HttpStreamAsyncResult
                {
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    Callback = cback,
                    State = state,
                    SynchRead = Math.Max(0, nread)
                };

                ares.Complete();
                return ares;
            }

            // Avoid reading past the end of the request to allow
            // for HTTP pipelining
            if (_remainingBody >= 0 && count > _remainingBody)
                count = (int)Math.Min(int.MaxValue, _remainingBody);
            return _stream.BeginRead(buffer, offset, count, cback, state);
        }

#if !NETSTANDARD1_6
        new 
#endif
        public int EndRead(IAsyncResult ares)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            if (ares == null)
                throw new ArgumentNullException(nameof(ares));

            var result = ares as HttpStreamAsyncResult;

            if (result != null)
            {
                var r = result;
                if (!ares.IsCompleted)
                    ares.AsyncWaitHandle.WaitOne();
                return r.SynchRead;
            }

            // Close on exception?
            var nread = _stream.EndRead(ares);
            if (_remainingBody > 0 && nread > 0)
                _remainingBody -= nread;
            return nread;
        }
#endif

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
#endif