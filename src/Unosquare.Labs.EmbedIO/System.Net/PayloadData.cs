namespace Unosquare.Net
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using Swan;
    
    internal class PayloadData : IEnumerable<byte>
    {
        public static readonly ulong MaxLength = long.MaxValue;

        private readonly byte[] _data;
        private readonly long _length;
        private ushort _code;
        private bool _codeSet;        
        private string _reason;
        private bool _reasonSet;
        
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

        internal byte[] ApplicationData => ExtensionDataLength > 0
            ? _data.SubArray(ExtensionDataLength, _length - ExtensionDataLength)
            : _data;

        internal byte[] ExtensionData => ExtensionDataLength > 0
            ? _data.SubArray(0, ExtensionDataLength)
            : WebSocket.EmptyBytes;

        internal ulong Length => (ulong)_length;

        internal ushort Code
        {
            get
            {
                if (!_codeSet)
                {
                    _code = _length > 1
                            ? BitConverter.ToUInt16(_data.SubArray(0, 2).ToHostOrder(Endianness.Big), 0)
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
                              ? _data.SubArray(2, _length - 2).ToText()
                              : string.Empty;

                    _reasonSet = true;
                }

                return _reason;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<byte> GetEnumerator() => ((IEnumerable<byte>)_data).GetEnumerator();

        public override string ToString() => BitConverter.ToString(_data);

        internal static byte[] Append(ushort code, string reason)
        {
            var ret = code.InternalToByteArray(Endianness.Big);
            if (string.IsNullOrEmpty(reason)) return ret;

            var buff = new List<byte>(ret);
            buff.AddRange(Encoding.UTF8.GetBytes(reason));
            ret = buff.ToArray();

            return ret;
        }

        internal void Mask(byte[] key)
        {
            for (long i = 0; i < _length; i++)
                _data[i] = (byte)(_data[i] ^ key[i % 4]);
        }

        internal byte[] ToArray() => _data;
    }
}