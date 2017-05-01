#if !NET46
#region License
/*
 * WebHeaderCollection.cs
 *
 * This code is derived from WebHeaderCollection.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2003 Ximian, Inc. (http://www.ximian.com)
 * Copyright (c) 2007 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2015 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

#region Authors
/*
 * Authors:
 * - Lawrence Pit <loz@cable.a2000.nl>
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 * - Miguel de Icaza <miguel@novell.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;

namespace Unosquare.Net
{
    [Flags]
    internal enum HttpHeaderType
    {
        Unspecified = 0,
        Request = 1,
        Response = 1 << 1,
        Restricted = 1 << 2,
        MultiValue = 1 << 3,
        MultiValueInRequest = 1 << 4,
        MultiValueInResponse = 1 << 5
    }

    internal class HttpHeaderInfo
    {
        #region Private Fields

        #endregion

        #region Internal Constructors

        internal HttpHeaderInfo(string name, HttpHeaderType type)
        {
            Name = name;
            Type = type;
        }

        #endregion

        #region Internal Properties

        internal bool IsMultiValueInRequest
            => (Type & HttpHeaderType.MultiValueInRequest) == HttpHeaderType.MultiValueInRequest;

        internal bool IsMultiValueInResponse
            => (Type & HttpHeaderType.MultiValueInResponse) == HttpHeaderType.MultiValueInResponse;

        #endregion

        #region Public Properties

        public bool IsRequest => (Type & HttpHeaderType.Request) == HttpHeaderType.Request;

        public bool IsResponse => (Type & HttpHeaderType.Response) == HttpHeaderType.Response;

        public string Name { get; }

        public HttpHeaderType Type { get; }

        #endregion

        #region Public Methods

        public bool IsMultiValue(bool response)
        {
            return (Type & HttpHeaderType.MultiValue) == HttpHeaderType.MultiValue
                ? (response ? IsResponse : IsRequest)
                : (response ? IsMultiValueInResponse : IsMultiValueInRequest);
        }

        public bool IsRestricted(bool response)
        {
            return (Type & HttpHeaderType.Restricted) == HttpHeaderType.Restricted &&
                   (response ? IsResponse : IsRequest);
        }

        #endregion
    }

    /// <summary>
    /// Provides a collection of the HTTP headers associated with a request or response.
    /// </summary>
    [ComVisible(true)]
    public class WebHeaderCollection : NameValueCollection
    {
        #region Private Fields

        private static readonly Dictionary<string, HttpHeaderInfo> Headers;
        private readonly bool _internallyUsed;

        #endregion

        #region Static Constructor

        static WebHeaderCollection()
        {
            Headers =
                new Dictionary<string, HttpHeaderInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Accept",
                        new HttpHeaderInfo(
                            "Accept",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptCharset",
                        new HttpHeaderInfo(
                            "Accept-Charset",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptEncoding",
                        new HttpHeaderInfo(
                            "Accept-Encoding",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptLanguage",
                        new HttpHeaderInfo(
                            "Accept-Language",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptRanges",
                        new HttpHeaderInfo(
                            "Accept-Ranges",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Age",
                        new HttpHeaderInfo(
                            "Age",
                            HttpHeaderType.Response)
                    },
                    {
                        "Allow",
                        new HttpHeaderInfo(
                            "Allow",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Authorization",
                        new HttpHeaderInfo(
                            "Authorization",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "CacheControl",
                        new HttpHeaderInfo(
                            "Cache-Control",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Connection",
                        new HttpHeaderInfo(
                            "Connection",
                            HttpHeaderType.Request |
                            HttpHeaderType.Response |
                            HttpHeaderType.Restricted |
                            HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentEncoding",
                        new HttpHeaderInfo(
                            "Content-Encoding",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentLanguage",
                        new HttpHeaderInfo(
                            "Content-Language",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentLength",
                        new HttpHeaderInfo(
                            "Content-Length",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "ContentLocation",
                        new HttpHeaderInfo(
                            "Content-Location",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentMd5",
                        new HttpHeaderInfo(
                            "Content-MD5",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentRange",
                        new HttpHeaderInfo(
                            "Content-Range",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentType",
                        new HttpHeaderInfo(
                            "Content-Type",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "Cookie",
                        new HttpHeaderInfo(
                            "Cookie",
                            HttpHeaderType.Request)
                    },
                    {
                        "Cookie2",
                        new HttpHeaderInfo(
                            "Cookie2",
                            HttpHeaderType.Request)
                    },
                    {
                        "Date",
                        new HttpHeaderInfo(
                            "Date",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "Expect",
                        new HttpHeaderInfo(
                            "Expect",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "Expires",
                        new HttpHeaderInfo(
                            "Expires",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ETag",
                        new HttpHeaderInfo(
                            "ETag",
                            HttpHeaderType.Response)
                    },
                    {
                        "From",
                        new HttpHeaderInfo(
                            "From",
                            HttpHeaderType.Request)
                    },
                    {
                        "Host",
                        new HttpHeaderInfo(
                            "Host",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "IfMatch",
                        new HttpHeaderInfo(
                            "If-Match",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "IfModifiedSince",
                        new HttpHeaderInfo(
                            "If-Modified-Since",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "IfNoneMatch",
                        new HttpHeaderInfo(
                            "If-None-Match",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "IfRange",
                        new HttpHeaderInfo(
                            "If-Range",
                            HttpHeaderType.Request)
                    },
                    {
                        "IfUnmodifiedSince",
                        new HttpHeaderInfo(
                            "If-Unmodified-Since",
                            HttpHeaderType.Request)
                    },
                    {
                        "KeepAlive",
                        new HttpHeaderInfo(
                            "Keep-Alive",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "LastModified",
                        new HttpHeaderInfo(
                            "Last-Modified",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "Location",
                        new HttpHeaderInfo(
                            "Location",
                            HttpHeaderType.Response)
                    },
                    {
                        "MaxForwards",
                        new HttpHeaderInfo(
                            "Max-Forwards",
                            HttpHeaderType.Request)
                    },
                    {
                        "Pragma",
                        new HttpHeaderInfo(
                            "Pragma",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ProxyAuthenticate",
                        new HttpHeaderInfo(
                            "Proxy-Authenticate",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ProxyAuthorization",
                        new HttpHeaderInfo(
                            "Proxy-Authorization",
                            HttpHeaderType.Request)
                    },
                    {
                        "ProxyConnection",
                        new HttpHeaderInfo(
                            "Proxy-Connection",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "Public",
                        new HttpHeaderInfo(
                            "Public",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Range",
                        new HttpHeaderInfo(
                            "Range",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "Referer",
                        new HttpHeaderInfo(
                            "Referer",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "RetryAfter",
                        new HttpHeaderInfo(
                            "Retry-After",
                            HttpHeaderType.Response)
                    },
                    {
                        "SecWebSocketAccept",
                        new HttpHeaderInfo(
                            "Sec-WebSocket-Accept",
                            HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "SecWebSocketExtensions",
                        new HttpHeaderInfo(
                            "Sec-WebSocket-Extensions",
                            HttpHeaderType.Request |
                            HttpHeaderType.Response |
                            HttpHeaderType.Restricted |
                            HttpHeaderType.MultiValueInRequest)
                    },
                    {
                        "SecWebSocketKey",
                        new HttpHeaderInfo(
                            "Sec-WebSocket-Key",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "SecWebSocketProtocol",
                        new HttpHeaderInfo(
                            "Sec-WebSocket-Protocol",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValueInRequest)
                    },
                    {
                        "SecWebSocketVersion",
                        new HttpHeaderInfo(
                            "Sec-WebSocket-Version",
                            HttpHeaderType.Request |
                            HttpHeaderType.Response |
                            HttpHeaderType.Restricted |
                            HttpHeaderType.MultiValueInResponse)
                    },
                    {
                        "Server",
                        new HttpHeaderInfo(
                            "Server",
                            HttpHeaderType.Response)
                    },
                    {
                        "SetCookie",
                        new HttpHeaderInfo(
                            "Set-Cookie",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "SetCookie2",
                        new HttpHeaderInfo(
                            "Set-Cookie2",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Te",
                        new HttpHeaderInfo(
                            "TE",
                            HttpHeaderType.Request)
                    },
                    {
                        "Trailer",
                        new HttpHeaderInfo(
                            "Trailer",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "TransferEncoding",
                        new HttpHeaderInfo(
                            "Transfer-Encoding",
                            HttpHeaderType.Request |
                            HttpHeaderType.Response |
                            HttpHeaderType.Restricted |
                            HttpHeaderType.MultiValue)
                    },
                    {
                        "Translate",
                        new HttpHeaderInfo(
                            "Translate",
                            HttpHeaderType.Request)
                    },
                    {
                        "Upgrade",
                        new HttpHeaderInfo(
                            "Upgrade",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "UserAgent",
                        new HttpHeaderInfo(
                            "User-Agent",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "Vary",
                        new HttpHeaderInfo(
                            "Vary",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Via",
                        new HttpHeaderInfo(
                            "Via",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Warning",
                        new HttpHeaderInfo(
                            "Warning",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "WwwAuthenticate",
                        new HttpHeaderInfo(
                            "WWW-Authenticate",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    }
                };
        }

        #endregion

        #region Internal Constructors

        internal WebHeaderCollection(HttpHeaderType state, bool internallyUsed)
        {
            State = state;
            _internallyUsed = internallyUsed;
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHeaderCollection"/> class.
        /// </summary>
        public WebHeaderCollection()
        {
        }

        internal void SetInternal(string key, string value)
        {
            AddWithoutValidate(key, value);
        }

        #endregion

        #region Internal Properties

        internal HttpHeaderType State { get; private set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the specified request <paramref name="header"/> in the collection.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the request <paramref name="header"/>.
        /// </value>
        /// <param name="header">
        /// One of the HttpRequestHeader enum values, represents
        /// the request header to get or set.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the request <paramref name="header"/>.
        /// </exception>
        public string this[System.Net.HttpRequestHeader header]
        {
            get { return Get(Convert(header)); }

            set { Add(header, value); }
        }

        /// <summary>
        /// Gets or sets the specified response <paramref name="header"/> in the collection.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the response <paramref name="header"/>.
        /// </value>
        /// <param name="header">
        /// One of the HttpResponseHeader enum values, represents
        /// the response header to get or set.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the response <paramref name="header"/>.
        /// </exception>
        public string this[System.Net.HttpResponseHeader header]
        {
            get { return Get(Convert(header)); }

            set { Add(header, value); }
        }

        #endregion

        #region Private Methods

        private void add(string name, string value, bool ignoreRestricted)
        {
            var act = ignoreRestricted
                ? (Action<string, string>) AddWithoutCheckingNameAndRestricted
                : AddWithoutCheckingName;

            DoWithCheckingState(act, CheckName(name), value, true);
        }

        private void AddWithoutCheckingName(string name, string value)
        {
            DoWithoutCheckingName(base.Add, name, value);
        }

        private void AddWithoutCheckingNameAndRestricted(string name, string value)
        {
            base.Add(name, CheckValue(value));
        }

        private static int CheckColonSeparated(string header)
        {
            var idx = header.IndexOf(':');
            if (idx == -1)
                throw new ArgumentException("No colon could be found.", nameof(header));

            return idx;
        }

        private static HttpHeaderType CheckHeaderType(string name)
        {
            var info = GetHeaderInfo(name);
            return info == null
                ? HttpHeaderType.Unspecified
                : info.IsRequest && !info.IsResponse
                    ? HttpHeaderType.Request
                    : !info.IsRequest && info.IsResponse
                        ? HttpHeaderType.Response
                        : HttpHeaderType.Unspecified;
        }

        private static string CheckName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            if (!IsHeaderName(name))
                throw new ArgumentException("Contains invalid characters.", nameof(name));

            return name;
        }

        private void CheckRestricted(string name)
        {
            if (!_internallyUsed && isRestricted(name, true))
                throw new ArgumentException("This header must be modified with the appropriate property.");
        }

        private void CheckState(bool response)
        {
            if (State == HttpHeaderType.Unspecified)
                return;

            if (response && State == HttpHeaderType.Request)
                throw new InvalidOperationException(
                    "This collection has already been used to store the request headers.");

            if (!response && State == HttpHeaderType.Response)
                throw new InvalidOperationException(
                    "This collection has already been used to store the response headers.");
        }

        private static string CheckValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Trim();
            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value), "Greater than 65,535 characters.");

            if (!IsHeaderValue(value))
                throw new ArgumentException("Contains invalid characters.", nameof(value));

            return value;
        }

        private static string convert(string key)
        {
            HttpHeaderInfo info;
            return Headers.TryGetValue(key, out info) ? info.Name : string.Empty;
        }

        private void DoWithCheckingState(
            Action<string, string> action, string name, string value, bool setState)
        {
            var type = CheckHeaderType(name);
            if (type == HttpHeaderType.Request)
                DoWithCheckingState(action, name, value, false, setState);
            else if (type == HttpHeaderType.Response)
                DoWithCheckingState(action, name, value, true, setState);
            else
                action(name, value);
        }

        private void DoWithCheckingState(
            Action<string, string> action, string name, string value, bool response, bool setState)
        {
            CheckState(response);
            action(name, value);
            if (setState && State == HttpHeaderType.Unspecified)
                State = response ? HttpHeaderType.Response : HttpHeaderType.Request;
        }

        private void DoWithoutCheckingName(Action<string, string> action, string name, string value)
        {
            CheckRestricted(name);
            action(name, CheckValue(value));
        }

        private static HttpHeaderInfo GetHeaderInfo(string name)
        {
            foreach (var info in Headers.Values)
                if (info.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return info;

            return null;
        }

        private static bool isRestricted(string name, bool response)
        {
            var info = GetHeaderInfo(name);
            return info != null && info.IsRestricted(response);
        }

        private void RemoveWithoutCheckingName(string name, string unuse)
        {
            CheckRestricted(name);
            base.Remove(name);
        }

        private void SetWithoutCheckingName(string name, string value)
        {
            DoWithoutCheckingName(base.Set, name, value);
        }

        #endregion

        #region Internal Methods

        internal static string Convert(System.Net.HttpRequestHeader header)
        {
            return convert(header.ToString());
        }

        internal static string Convert(System.Net.HttpResponseHeader header)
        {
            return convert(header.ToString());
        }

        internal void InternalRemove(string name)
        {
            base.Remove(name);
        }

        internal void InternalSet(string header, bool response)
        {
            var pos = CheckColonSeparated(header);
            InternalSet(header.Substring(0, pos), header.Substring(pos + 1), response);
        }

        internal void InternalSet(string name, string value, bool response)
        {
            value = CheckValue(value);
            if (IsMultiValue(name, response))
                base.Add(name, value);
            else
                base.Set(name, value);
        }

        internal static bool IsHeaderName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.IsToken();
        }

        internal static bool IsHeaderValue(string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                    return false;

                if (c == 0x7f)
                    return false;

                if (c == '\n' && ++i < len)
                {
                    c = value[i];
                    if (!" \t".Contains(c))
                        return false;
                }
            }

            return true;
        }

        internal static bool IsMultiValue(string headerName, bool response)
        {
            if (string.IsNullOrEmpty(headerName))
                return false;

            var info = GetHeaderInfo(headerName);
            return info != null && info.IsMultiValue(response);
        }
        
        #endregion

        #region Protected Methods

        /// <summary>
        /// Adds a header to the collection without checking if the header is on
        /// the restricted header list.
        /// </summary>
        /// <param name="headerName">
        /// A <see cref="string"/> that represents the name of the header to add.
        /// </param>
        /// <param name="headerValue">
        /// A <see cref="string"/> that represents the value of the header to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="headerName"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="headerName"/> or <paramref name="headerValue"/> contains invalid characters.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="headerValue"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the <paramref name="headerName"/>.
        /// </exception>
        public void AddWithoutValidate(string headerName, string headerValue)
        {
            add(headerName, headerValue, true);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified <paramref name="header"/> to the collection.
        /// </summary>
        /// <param name="header">
        /// A <see cref="string"/> that represents the header with the name and value separated by
        /// a colon (<c>':'</c>).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="header"/> is <see langword="null"/>, empty, or the name part of
        /// <paramref name="header"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> doesn't contain a colon.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The name or value part of <paramref name="header"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of the value part of <paramref name="header"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the <paramref name="header"/>.
        /// </exception>
        public void Add(string header)
        {
            if (string.IsNullOrEmpty(header))
                throw new ArgumentNullException(nameof(header));

            var pos = CheckColonSeparated(header);
            add(header.Substring(0, pos), header.Substring(pos + 1), false);
        }

        /// <summary>
        /// Adds the specified request <paramref name="header"/> with
        /// the specified <paramref name="value"/> to the collection.
        /// </summary>
        /// <param name="header">
        /// One of theHttpRequestHeader enum values, represents
        /// the request header to add.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to add.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the request <paramref name="header"/>.
        /// </exception>
        public void Add(System.Net.HttpRequestHeader header, string value)
        {
            DoWithCheckingState(AddWithoutCheckingName, Convert(header), value, false, true);
        }

        /// <summary>
        /// Adds the specified response <paramref name="header"/> with
        /// the specified <paramref name="value"/> to the collection.
        /// </summary>
        /// <param name="header">
        /// One of the <see cref="System.Net.HttpResponseHeader"/> enum values, represents
        /// the response header to add.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to add.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the response <paramref name="header"/>.
        /// </exception>
        public void Add(System.Net.HttpResponseHeader header, string value)
        {
            DoWithCheckingState(AddWithoutCheckingName, Convert(header), value, true, true);
        }

        /// <summary>
        /// Adds a header with the specified <paramref name="name"/> and
        /// <paramref name="value"/> to the collection.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string"/> that represents the name of the header to add.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> or <paramref name="value"/> contains invalid characters.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> is a restricted header name.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the header <paramref name="name"/>.
        /// </exception>
        public override void Add(string name, string value)
        {
            add(name, value, false);
        }

        /// <summary>
        /// Removes all headers from the collection.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            State = HttpHeaderType.Unspecified;
        }

        /// <summary>
        /// Gets an array of header values stored in the specified <paramref name="index"/> position of
        /// the collection.
        /// </summary>
        /// <returns>
        /// An array of <see cref="string"/> that receives the header values if found;
        /// otherwise, <see langword="null"/>.
        /// </returns>
        /// <param name="index">
        /// An <see cref="int"/> that represents the zero-based index of the header to find.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of allowable range of indexes for the collection.
        /// </exception>
        public override string[] GetValues(int index)
        {
            var vals = base.GetValues(index);
            return vals != null && vals.Length > 0 ? vals : null;
        }

        /// <summary>
        /// Gets an array of header values stored in the specified <paramref name="header"/>.
        /// </summary>
        /// <returns>
        /// An array of <see cref="string"/> that receives the header values if found;
        /// otherwise, <see langword="null"/>.
        /// </returns>
        /// <param name="header">
        /// A <see cref="string"/> that represents the name of the header to find.
        /// </param>
        public override string[] GetValues(string header)
        {
            var vals = base.GetValues(header);
            return vals != null && vals.Length > 0 ? vals : null;
        }

        /// <summary>
        /// Determines whether the specified header can be set for the request.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the header is restricted; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="headerName">
        /// A <see cref="string"/> that represents the name of the header to test.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="headerName"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="headerName"/> contains invalid characters.
        /// </exception>
        public static bool IsRestricted(string headerName)
        {
            return isRestricted(CheckName(headerName), false);
        }

        /// <summary>
        /// Determines whether the specified header can be set for the request or the response.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the header is restricted; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="headerName">
        /// A <see cref="string"/> that represents the name of the header to test.
        /// </param>
        /// <param name="response">
        /// <c>true</c> if does the test for the response; for the request, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="headerName"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="headerName"/> contains invalid characters.
        /// </exception>
        public static bool IsRestricted(string headerName, bool response)
        {
            return isRestricted(CheckName(headerName), response);
        }

        /// <summary>
        /// Removes the specified request <paramref name="header"/> from the collection.
        /// </summary>
        /// <param name="header">
        /// One of theHttpRequestHeader enum values, represents
        /// the request header to remove.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="header"/> is a restricted header.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the request <paramref name="header"/>.
        /// </exception>
        public void Remove(System.Net.HttpRequestHeader header)
        {
            DoWithCheckingState(RemoveWithoutCheckingName, Convert(header), null, false, false);
        }

        /// <summary>
        /// Removes the specified response <paramref name="header"/> from the collection.
        /// </summary>
        /// <param name="header">
        /// One of the <see cref="System.Net.HttpResponseHeader"/> enum values, represents
        /// the response header to remove.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="header"/> is a restricted header.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the response <paramref name="header"/>.
        /// </exception>
        public void Remove(System.Net.HttpResponseHeader header)
        {
            DoWithCheckingState(RemoveWithoutCheckingName, Convert(header), null, true, false);
        }

        /// <summary>
        /// Removes the specified header from the collection.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string"/> that represents the name of the header to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> contains invalid characters.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> is a restricted header name.
        ///   </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the header <paramref name="name"/>.
        /// </exception>
        public override void Remove(string name)
        {
            DoWithCheckingState(RemoveWithoutCheckingName, CheckName(name), null, false);
        }

        /// <summary>
        /// Sets the specified request <paramref name="header"/> to the specified value.
        /// </summary>
        /// <param name="header">
        /// One of theHttpRequestHeader enum values, represents
        /// the request header to set.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the request header to set.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the request <paramref name="header"/>.
        /// </exception>
        public void Set(System.Net.HttpRequestHeader header, string value)
        {
            DoWithCheckingState(SetWithoutCheckingName, Convert(header), value, false, true);
        }

        /// <summary>
        /// Sets the specified response <paramref name="header"/> to the specified value.
        /// </summary>
        /// <param name="header">
        /// One of the <see cref="System.Net.HttpResponseHeader"/> enum values, represents
        /// the response header to set.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the response header to set.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="header"/> is a restricted header.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> contains invalid characters.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the response <paramref name="header"/>.
        /// </exception>
        public void Set(System.Net.HttpResponseHeader header, string value)
        {
            DoWithCheckingState(SetWithoutCheckingName, Convert(header), value, true, true);
        }

        /// <summary>
        /// Sets the specified header to the specified value.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string"/> that represents the name of the header to set.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the header to set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> or <paramref name="value"/> contains invalid characters.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> is a restricted header name.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The length of <paramref name="value"/> is greater than 65,535 characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The current <see cref="WebHeaderCollection"/> instance doesn't allow
        /// the header <paramref name="name"/>.
        /// </exception>
        public override void Set(string name, string value)
        {
            DoWithCheckingState(SetWithoutCheckingName, CheckName(name), value, true);
        }
        
        /// <summary>
        /// Returns a <see cref="string"/> that represents the current
        /// <see cref="WebHeaderCollection"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current <see cref="WebHeaderCollection"/>.
        /// </returns>
        public override string ToString()
        {
            var buff = new StringBuilder();

            foreach (string key in Keys)
                buff.AppendFormat("{0}: {1}\r\n", key, Get(key));

            return buff.Append("\r\n").ToString();
        }

        #endregion
    }
}

#endif
