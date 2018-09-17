namespace Unosquare.Net
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Swan;
    
    internal class PayloadData 
    {
        public static readonly ulong MaxLength = long.MaxValue;

        private readonly byte[] _data;
        private ushort? _code;
        private string _reason;
        private bool _reasonSet;
        
        internal PayloadData(byte[] data)
        {
            _data = data;
        }
        
        internal PayloadData(ushort code = 1005, string reason = null)
        {
            _code = code;
            _reason = reason ?? string.Empty;

            _data = code == 1005 ? WebSocket.EmptyBytes : Append(code, reason);

            _reasonSet = true;
        }

        internal byte[] ApplicationData => ExtensionDataLength > 0
            ? _data.SubArray(ExtensionDataLength, _data.Length - ExtensionDataLength)
            : _data;

        internal byte[] ExtensionData => ExtensionDataLength > 0
            ? _data.SubArray(0, ExtensionDataLength)
            : WebSocket.EmptyBytes;

        internal ulong Length => (ulong)_data.Length;

        internal ushort Code
        {
            get
            {
                if (!_code.HasValue)
                {
                    _code = _data.Length > 1
                            ? BitConverter.ToUInt16(_data.SubArray(0, 2).ToHostOrder(Endianness.Big), 0)
                            : (ushort)1005;
                }

                return _code.Value;
            }
        }

        internal long ExtensionDataLength { get; set; }

        internal bool HasReservedCode => _data.Length > 1 && (Code == (ushort)CloseStatusCode.Undefined ||
                   Code == (ushort)CloseStatusCode.NoStatus ||
                   Code == (ushort)CloseStatusCode.Abnormal ||
                   Code == (ushort)CloseStatusCode.TlsHandshakeFailure);

        internal string Reason
        {
            get
            {
                if (!_reasonSet)
                {
                    _reason = _data.Length > 2
                              ? _data.SubArray(2, _data.Length - 2).ToText()
                              : string.Empty;

                    _reasonSet = true;
                }

                return _reason;
            }
        }
        
        public override string ToString() => BitConverter.ToString(_data);

        internal static byte[] Append(ushort code, string reason)
        {
            var ret = code.ToByteArray(Endianness.Big);
            if (string.IsNullOrEmpty(reason)) return ret;

            var buff = new List<byte>(ret);
            buff.AddRange(Encoding.UTF8.GetBytes(reason));

            return buff.ToArray();
        }

        internal void Mask(byte[] key)
        {
            for (long i = 0; i < _data.Length; i++)
                _data[i] = (byte)(_data[i] ^ key[i % 4]);
        }

        internal byte[] ToArray() => _data;
    }
}