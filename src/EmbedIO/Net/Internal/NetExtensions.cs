﻿using System;
using System.Linq;
using EmbedIO.Constants;
using Unosquare.Swan;

namespace EmbedIO.Net.Internal
{
    /// <summary>
    /// Represents some System.NET custom extensions.
    /// </summary>
    internal static class NetExtensions
    {
        internal static byte[] ToByteArray(this ushort value, Endianness order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] ToByteArray(this ulong value, Endianness order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }
        
        internal static byte[] ToHostOrder(this byte[] source, Endianness sourceOrder)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }
        
        internal static bool IsHostOrder(this Endianness order)
        {
            // true: !(true ^ true) or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == Endianness.Little));
        }
        
        internal static string ToExtensionString(this CompressionMethod method, params string[] parameters)
        {
            if (method == CompressionMethod.None)
                return string.Empty;

            var m = $"permessage-{method.ToString().ToLowerInvariant()}";

            return parameters == null || parameters.Length == 0 ? m : $"{m}; {string.Join("; ", parameters)}";
        }
    }
}