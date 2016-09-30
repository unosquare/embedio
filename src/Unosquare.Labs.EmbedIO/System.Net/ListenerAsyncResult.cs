#if !NET452
//
// System.Net.ListenerAsyncResult
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (c) 2005 Ximian, Inc (http://www.ximian.com)
//

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

using System.Threading;

namespace System.Net
{
    internal class ListenerAsyncResult : IAsyncResult
    {
        private ManualResetEvent _handle;
        private bool _synch;
        private bool _completed;
        private readonly AsyncCallback _cb;
        private readonly object _state;
        private Exception _exception;
        private HttpListenerContext _context;
        private readonly object _locker = new object();
        private ListenerAsyncResult _forward;
        internal bool EndCalled;
        internal bool InGet;

        public ListenerAsyncResult(AsyncCallback cb, object state)
        {
            _cb = cb;
            _state = state;
        }

        internal void Complete(Exception exc)
        {
            if (_forward != null)
            {
                _forward.Complete(exc);
                return;
            }
            _exception = exc;
            if (InGet && (exc is ObjectDisposedException))
                _exception = new HttpListenerException(500, "Listener closed");
            lock (_locker)
            {
                _completed = true;
                _handle?.Set();

                if (_cb != null)
                    ThreadPool.QueueUserWorkItem(_invokeCb, this);
            }
        }

        private static readonly WaitCallback _invokeCb = InvokeCallback;

        private static void InvokeCallback(object o)
        {
            var ares = (ListenerAsyncResult) o;
            if (ares._forward != null)
            {
                InvokeCallback(ares._forward);
                return;
            }
            try
            {
                ares._cb(ares);
            }
            catch
            {
            }
        }

        internal void Complete(HttpListenerContext context)
        {
            Complete(context, false);
        }

        internal void Complete(HttpListenerContext context, bool synch)
        {
            if (_forward != null)
            {
                _forward.Complete(context, synch);
                return;
            }
            _synch = synch;
            _context = context;
            lock (_locker)
            {
                var schemes = context.Listener.SelectAuthenticationScheme(context);
                if ((schemes == AuthenticationSchemes.Basic ||
                     context.Listener.AuthenticationSchemes == AuthenticationSchemes.Negotiate) &&
                    context.Request.Headers["Authorization"] == null)
                {
                    context.Response.StatusCode = 401;
                    context.Response.Headers["WWW-Authenticate"] = schemes + " realm=\"" + context.Listener.Realm + "\"";
                    context.Response.OutputStream.Dispose();
                    var ares = context.Listener.BeginGetContext(_cb, _state);
                    _forward = (ListenerAsyncResult) ares;
                    lock (_forward._locker)
                    {
                        if (_handle != null)
                            _forward._handle = _handle;
                    }
                    var next = _forward;
                    for (var i = 0; next._forward != null; i++)
                    {
                        if (i > 20)
                            Complete(new HttpListenerException(400, "Too many authentication errors"));
                        next = next._forward;
                    }
                }
                else
                {
                    _completed = true;
                    _synch = false;

                    _handle?.Set();

                    if (_cb != null)
                        ThreadPool.QueueUserWorkItem(_invokeCb, this);
                }
            }
        }

        internal HttpListenerContext GetContext()
        {
            if (_forward != null)
                return _forward.GetContext();
            if (_exception != null)
                throw _exception;

            return _context;
        }

        public object AsyncState
        {
            get
            {
                if (_forward != null)
                    return _forward.AsyncState;
                return _state;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_forward != null)
                    return _forward.AsyncWaitHandle;

                lock (_locker)
                {
                    if (_handle == null)
                        _handle = new ManualResetEvent(_completed);
                }

                return _handle;
            }
        }

        public bool CompletedSynchronously => _forward?.CompletedSynchronously ?? _synch;

        public bool IsCompleted
        {
            get
            {
                if (_forward != null)
                    return _forward.IsCompleted;

                lock (_locker)
                {
                    return _completed;
                }
            }
        }
    }
}

#endif