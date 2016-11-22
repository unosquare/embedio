#if !NET46
//
// System.Net.HttpListenerContext
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
using System.Security.Principal;
using Unosquare.Labs.EmbedIO.Log;

namespace Unosquare.Net
{
    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class. This class cannot be inherited.
    /// </summary>
    public sealed class HttpListenerContext
    {
        internal HttpListener Listener;

        private WebSocketContext _websocketContext;

        internal HttpListenerContext(HttpConnection cnc)
        {
            Connection = cnc;
            Request = new HttpListenerRequest(this);
            Response = new HttpListenerResponse(this);
        }

        internal int ErrorStatus { get; set; } = 400;

        internal string ErrorMessage { get; set; }

        internal bool HaveError => (ErrorMessage != null);

        internal HttpConnection Connection { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public HttpListenerRequest Request { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public HttpListenerResponse Response { get; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        public IPrincipal User { get; private set; }

#if AUTHENTICATION
        internal void ParseAuthentication(AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == AuthenticationSchemes.Anonymous)
                return;

            // TODO: Handle NTLM/Digest modes
            var header = Request.Headers["Authorization"];
            if (header == null || header.Length < 2)
                return;

            var authenticationData = header.Split(new[] {' '}, 2);
            if (string.Compare(authenticationData[0], "basic", StringComparison.OrdinalIgnoreCase) == 0)
            {
                User = ParseBasicAuthentication(authenticationData[1]);
            }
            // TODO: throw if malformed -> 400 bad request
        }

        internal IPrincipal ParseBasicAuthentication(string authData)
        {
            try
            {
                // Basic AUTH Data is a formatted Base64 String
                //string domain = null;
                var authString = Encoding.GetEncoding(0).GetString(Convert.FromBase64String(authData));

                // The format is DOMAIN\username:password
                // Domain is optional

                var pos = authString.IndexOf(':');

                // parse the password off the end
                var password = authString.Substring(pos + 1);

                // discard the password
                authString = authString.Substring(0, pos);

                // check if there is a domain
                pos = authString.IndexOf('\\');

                var user = pos > 0 ? authString.Substring(pos) : authString;

                var identity = new HttpListenerBasicIdentity(user, password);
                // TODO: What are the roles MS sets
                return new GenericPrincipal(identity, new string[0]);
            }
            catch (Exception)
            {
                // Invalid auth data is swallowed silently
                return null;
            }
        }
#endif
        
        /// <summary>
        /// Accepts a WebSocket handshake request.
        /// </summary>
        /// <returns>
        /// A <see cref="HttpListenerWebSocketContext"/> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <param name="protocol">
        /// A <see cref="string"/> that represents the subprotocol supported on
        /// this WebSocket connection.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="protocol"/> is empty.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocol"/> contains an invalid character.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This method has already been called.
        /// </exception>
        public WebSocketContext AcceptWebSocket(string protocol, ILog log)
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            if (protocol != null)
            {
                if (protocol.Length == 0)
                    throw new ArgumentException("An empty string.", "protocol");

                //if (!protocol.IsToken())
                //    throw new ArgumentException("Contains an invalid character.", "protocol");
            }

            _websocketContext = new WebSocketContext(this, protocol, log);
            return _websocketContext;
        }
    }
}

#endif