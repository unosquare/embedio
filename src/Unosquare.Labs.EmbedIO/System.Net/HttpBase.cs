#if !NET46
#region License
/*
 * HttpBase.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan;

namespace Unosquare.Net
{
    internal abstract class HttpBase
    {
        #region Private Fields

        private const int HeadersMaxLength = 8192;

        #endregion

        #region Internal Fields

        internal byte[] EntityBodyData;

        #endregion

        #region Protected Fields

        protected const string CrLf = "\r\n";

        #endregion

        #region Protected Constructors

        protected HttpBase(Version version, NameValueCollection headers)
        {
            ProtocolVersion = version;
            Headers = headers;
        }

        #endregion

        #region Public Properties

        public string EntityBody
        {
            get
            {
                if (EntityBodyData == null || EntityBodyData.Length == 0)
                    return string.Empty;

                Encoding enc = null;

                var contentType = Headers["Content-Type"];
                if (!string.IsNullOrEmpty(contentType))
                    enc = GetEncoding(contentType);

                return (enc ?? Encoding.UTF8).GetString(EntityBodyData);
            }
        }

        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        #endregion

        #region Private Methods

        internal static string GetValue(string nameAndValue, char separator, bool unquote)
        {
            var idx = nameAndValue.IndexOf(separator);
            if (idx < 0 || idx == nameAndValue.Length - 1)
                return null;

            var val = nameAndValue.Substring(idx + 1).Trim();
            return unquote ? val.Unquote() : val;
        }

        internal static Encoding GetEncoding(string contentType)
        {
            var parts = contentType.Split(';');

            foreach (var p in parts)
            {
                var part = p.Trim();
                if (part.StartsWith("charset", StringComparison.OrdinalIgnoreCase))
                    return Encoding.GetEncoding(GetValue(part, '=', true));
            }

            return null;
        }

        private static async Task<byte[]> ReadEntityBodyAsync(Stream stream, string length, CancellationToken ct)
        {
            long len;
            if (!long.TryParse(length, out len))
                throw new ArgumentException("Cannot be parsed.", nameof(length));

            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Less than zero.");

            return len > 1024
                   ? await stream.ReadBytesAsync(len, 1024, ct)
                   : len > 0
                     ? await stream.ReadBytesAsync((int)len, ct)
                     : null;
        }

        private static bool EqualsWith(int value, char c, Action<int> action)
        {
            action(value);
            return value == c - 0;
        }

        private static string[] ReadHeaders(Stream stream, int maxLength)
        {
            var buff = new List<byte>();
            var cnt = 0;
            Action<int> add = i =>
            {
                if (i == -1)
                    throw new EndOfStreamException("The header cannot be read from the data source.");

                buff.Add((byte)i);
                cnt++;
            };

            var read = false;
            while (cnt < maxLength)
            {
                if (EqualsWith(stream.ReadByte(), '\r', add) &&
                    EqualsWith(stream.ReadByte(), '\n', add) &&
                    EqualsWith(stream.ReadByte(), '\r', add) &&
                    EqualsWith(stream.ReadByte(), '\n', add))
                {
                    read = true;
                    break;
                }
            }

            if (!read)
                throw new WebSocketException("The length of header part is greater than the max length.");

            return Encoding.UTF8.GetString(buff.ToArray())
                   .Replace(CrLf + " ", " ")
                   .Replace(CrLf + "\t", " ")
                   .Split(new[] { CrLf }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion

        #region Protected Methods

        protected static async Task<T> ReadAsync<T>(Stream stream, Func<string[], T> parser, int millisecondsTimeout, CancellationToken ct = default(CancellationToken))
          where T : HttpBase
        {
            var timeout = false;
            var timer = new Timer(
              state =>
              {
                  timeout = true;
#if NET452
                  stream.Close();
#else
                  stream.Dispose();
#endif
              },
              null,
              millisecondsTimeout,
              -1);

            try
            {
                var http = parser(ReadHeaders(stream, HeadersMaxLength));
                var contentLen = http.Headers["Content-Length"];

                if (!string.IsNullOrEmpty(contentLen))
                    http.EntityBodyData = await ReadEntityBodyAsync(stream, contentLen, ct);

                return http;
            }
            catch (Exception ex)
            {
                throw new WebSocketException(timeout
                      ? "A timeout has occurred while reading an HTTP request/response."
                      : "An exception has occurred while reading an HTTP request/response.", ex);
            }
            finally
            {
                timer.Change(-1, -1);
                timer.Dispose();
            }
        }

        #endregion

        #region Public Methods

        public byte[] ToByteArray() => Encoding.UTF8.GetBytes(ToString());

        public void Write(byte[] data)
        {
            EntityBodyData = data;
        }

        #endregion
    }
}
#endif