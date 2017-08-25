#if !NET47
//
// System.Net.HttpListenerContext
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
    using System.Threading.Tasks;

    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class. This class cannot be inherited.
    /// </summary>
    public sealed class HttpListenerContext
    {
        private WebSocketContext _websocketContext;

        internal HttpListenerContext(HttpConnection cnc)
        {
            Id = Guid.NewGuid();
            Connection = cnc;
            Request = new HttpListenerRequest(this);
            Response = new HttpListenerResponse(this);
        }

        /// <summary>
        /// Gets the request.
        /// </summary>
        public HttpListenerRequest Request { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public HttpListenerResponse Response { get; }
        
        internal HttpListener Listener { get; set; }

        internal int ErrorStatus { get; set; } = 400;

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }

        internal Guid Id { get; }

        /// <summary>
        /// Accepts a WebSocket handshake request.
        /// </summary>
        /// <returns>
        /// A <see cref="WebSocketContext" /> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <exception cref="InvalidOperationException">This method has already been called.</exception>
        public async Task<WebSocketContext> AcceptWebSocketAsync()
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            _websocketContext = new WebSocketContext(this);
            await _websocketContext.WebSocket.InternalAcceptAsync();

            return _websocketContext;
        }
    }
}

#endif