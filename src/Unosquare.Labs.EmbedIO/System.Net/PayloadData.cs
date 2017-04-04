#if !NET46
#region License
/*
 * PayloadData.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2016 sta.blockhead
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unosquare.Swan;

namespace Unosquare.Net
{
    internal class PayloadData : IEnumerable<byte>
    {
        #region Private Fields

        private ushort _code;
        private bool _codeSet;
        private readonly byte[] _data;
        private readonly long _length;
        private string _reason;
        private bool _reasonSet;

        #endregion

        #region Public Fields
        
        /// <summary>
        /// Represents the allowable max length.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   A <see cref="WebSocketException"/> will occur if the payload data length is
        ///   greater than the value of this field.
        ///   </para>
        ///   <para>
        ///   If you would like to change the value, you must set it to a value between
        ///   <c>WebSocket.FragmentLength</c> and <c>Int64.MaxValue</c> inclusive.
        ///   </para>
        /// </remarks>
        public static readonly ulong MaxLength;

        #endregion

        #region Static Constructor

        static PayloadData()
        {
            MaxLength = Int64.MaxValue;
        }

        #endregion

        #region Internal Constructors

        internal PayloadData()
        {
            _code = 1005;
            _reason = string.Empty;

            _data = WebSocket.EmptyBytes;

            _codeSet = true;
            _reasonSet = true;
        }

        internal PayloadData(byte[] data)
          : this(data, data.Length)
        {
        }

        internal PayloadData(byte[] data, long length)
        {
            _data = data;
            _length = length;
        }

        internal PayloadData(ushort code, string reason)
        {
            _code = code;
            _reason = reason ?? string.Empty;

            _data = Append(code, reason);
            _length = _data.Length;

            _codeSet = true;
            _reasonSet = true;
        }

        internal static byte[] Append(ushort code, string reason)
        {
            var ret = code.InternalToByteArray(Endianness.Big);
            if (string.IsNullOrEmpty(reason)) return ret;

            var buff = new List<byte>(ret);
            buff.AddRange(Encoding.UTF8.GetBytes(reason));
            ret = buff.ToArray();

            return ret;
        }

        #endregion

        #region Internal Properties

        internal ushort Code
        {
            get
            {
                if (!_codeSet)
                {
                    _code = _length > 1
                            ? BitConverter.ToUInt16(_data.SubArray(0, 2).ToHostOrder(Swan.Endianness.Big), 0)
                            : (ushort)1005;

                    _codeSet = true;
                }

                return _code;
            }
        }

        internal long ExtensionDataLength { get; set; }

        internal bool HasReservedCode => _length > 1 && (Code == (ushort)CloseStatusCode.Undefined ||
                   Code == (ushort)CloseStatusCode.NoStatus ||
                   Code == (ushort)CloseStatusCode.Abnormal ||
                   Code == (ushort)CloseStatusCode.TlsHandshakeFailure);

        internal string Reason
        {
            get
            {
                if (!_reasonSet)
                {
                    _reason = _length > 2
                              ? Encoding.UTF8.GetString(_data.SubArray(2, _length - 2))
                              : string.Empty;

                    _reasonSet = true;
                }

                return _reason;
            }
        }

        #endregion

        #region Public Properties

        public byte[] ApplicationData => ExtensionDataLength > 0
            ? _data.SubArray(ExtensionDataLength, _length - ExtensionDataLength)
            : _data;

        public byte[] ExtensionData => ExtensionDataLength > 0
            ? _data.SubArray(0, ExtensionDataLength)
            : WebSocket.EmptyBytes;

        public ulong Length => (ulong)_length;

        #endregion

        #region Internal Methods

        internal void Mask(byte[] key)
        {
            for (long i = 0; i < _length; i++)
                _data[i] = (byte)(_data[i] ^ key[i % 4]);
        }

        #endregion

        #region Public Methods

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_data).GetEnumerator();
        }

        public byte[] ToArray()
        {
            return _data;
        }

        public override string ToString()
        {
            return BitConverter.ToString(_data);
        }

        #endregion

        #region Explicit Interface Implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
#endif