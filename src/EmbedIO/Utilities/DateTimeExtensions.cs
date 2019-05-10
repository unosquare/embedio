using System;
using System.Globalization;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> to the <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#RFC1123">RFC1123 format</see>.
        /// </summary>
        /// <param name="this">The <see cref="DateTime"/> on which this method is called.</param>
        /// <returns>The string representation of <paramref name="this"/> according to <see href="https://tools.ietf.org/html/rfc1123#page-54">RFC1123</see>.</returns>
        /// <remarks>
        /// <para>If <paramref name="this"/> is not a UTC date / time, its UTC equivalent is converted, leaving <paramref name="this"/> unchanged.</para>
        /// </remarks>
        public static string ToRfc1123String(this DateTime @this)
            => @this.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
    }
}