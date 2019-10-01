using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swan;
using EmbedIO.Net.Internal;

namespace EmbedIO.WebSockets.Internal
{
    internal class PayloadData
    {
        public const ulong MaxLength = long.MaxValue;

        private readonly byte[] _data;
        private ushort? _code;

        internal PayloadData(byte[] data)
        {
            _data = data;
        }

        internal PayloadData(ushort code = 1005, string? reason = null)
        {
            _code = code;
            _data = code == 1005 ? Array.Empty<byte>() : Append(code, reason);
        }

        internal MemoryStream ApplicationData => new MemoryStream(_data);

        internal ulong Length => (ulong)_data.Length;

        internal ushort Code
        {
            get
            {
                if (!_code.HasValue)
                {
                    _code = _data.Length > 1
                            ? BitConverter.ToUInt16(_data.Take(2).ToArray().ToHostOrder(Endianness.Big), 0)
                            : (ushort)1005;
                }

                return _code.Value;
            }
        }

        internal bool HasReservedCode => _data.Length > 1 && (Code == (ushort)CloseStatusCode.Undefined ||
                   Code == (ushort)CloseStatusCode.NoStatus ||
                   Code == (ushort)CloseStatusCode.Abnormal ||
                   Code == (ushort)CloseStatusCode.TlsHandshakeFailure);

        public override string ToString() => BitConverter.ToString(_data);

        internal static byte[] Append(ushort code, string? reason)
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