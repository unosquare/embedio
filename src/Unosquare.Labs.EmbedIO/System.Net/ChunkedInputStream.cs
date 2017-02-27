#if CHUNKED
#if !NET46
//
// System.Net.ChunkedInputStream
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Unosquare.Net
{
    internal class ChunkedInputStream : RequestStream
    {
        private bool _disposed;
        private bool _noMoreData;

        private class ReadBufferState
        {
            public readonly byte[] Buffer;
            public int Offset;
            public int Count;
            public int InitialCount;
            public readonly HttpStreamAsyncResult Ares;

            public ReadBufferState(byte[] buffer, int offset, int count,
                HttpStreamAsyncResult ares)
            {
                Buffer = buffer;
                Offset = offset;
                Count = count;
                InitialCount = count;
                Ares = ares;
            }
        }

        public ChunkedInputStream(HttpListenerContext context, Stream stream,
            byte[] buffer, int offset, int length)
            : base(stream, buffer, offset, length)
        {
            Decoder = new ChunkStream(context.Request.Headers);
        }

        public ChunkStream Decoder { get; set; }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            var ares = BeginRead(buffer, offset, count, null, null);
            return EndRead(ares);
        }

        public new IAsyncResult BeginRead(byte[] buffer, int offset, int count,
            AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var len = buffer.Length;
            if (offset < 0 || offset > len)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset exceeds the size of buffer");

            if (count < 0 || offset > len - count)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset+size exceeds the size of buffer");

            var ares = new HttpStreamAsyncResult
            {
                Callback = cback,
                State = state
            };

            if (_noMoreData)
            {
                ares.Complete();
                return ares;
            }

            var nread = Decoder.Read(buffer, offset, count);
            offset += nread;
            count -= nread;
            if (count == 0)
            {
                // got all we wanted, no need to bother the decoder yet
                ares.Count = nread;
                ares.Complete();
                return ares;
            }
            if (!Decoder.WantMore)
            {
                _noMoreData = nread == 0;
                ares.Count = nread;
                ares.Complete();
                return ares;
            }
            ares.Buffer = new byte[8192];
            ares.Offset = 0;
            ares.Count = 8192;
            var rb = new ReadBufferState(buffer, offset, count, ares);
            rb.InitialCount += nread;
            base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
            return ares;
        }

        private void OnRead(IAsyncResult baseAres)
        {
            var rb = (ReadBufferState)baseAres.AsyncState;
            var ares = rb.Ares;
            try
            {
                var nread = base.EndRead(baseAres);
                Decoder.Write(ares.Buffer, ares.Offset, nread);
                nread = Decoder.Read(rb.Buffer, rb.Offset, rb.Count);
                rb.Offset += nread;
                rb.Count -= nread;
                if (rb.Count == 0 || !Decoder.WantMore || nread == 0)
                {
                    _noMoreData = !Decoder.WantMore && nread == 0;
                    ares.Count = rb.InitialCount - rb.Count;
                    ares.Complete();
                    return;
                }
                ares.Offset = 0;
                ares.Count = Math.Min(8192, Decoder.ChunkLeft + 6);
                base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
            }
            catch (Exception e)
            {
                ares.Complete(e);
            }
        }

        public new int EndRead(IAsyncResult ares)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            var myAres = ares as HttpStreamAsyncResult;
            if (ares == null)
                throw new ArgumentException("Invalid IAsyncResult", nameof(ares));

            if (!ares.IsCompleted)
                ares.AsyncWaitHandle.WaitOne();

            if (myAres?.Error != null)
                throw new HttpListenerException(400, "I/O operation aborted: " + myAres.Error.Message);

            return myAres.Count;
        }

#if !NETSTANDARD1_6
        new 
#endif
        public void Close()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose();
            }
        }
    }
}
#endif
#endif