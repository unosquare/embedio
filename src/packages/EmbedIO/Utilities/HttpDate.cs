using System;
using System.Globalization;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides standard methods to parse and format <see cref="DateTime"/>s according to various RFCs.
    /// </summary>
    public static class HttpDate
    {
        // https://github.com/dotnet/corefx/blob/master/src/Common/src/System/Net/HttpDateParser.cs
        private static readonly string[] DateFormats = {
            // "r", // RFC 1123, required output format but too strict for input
            "ddd, d MMM yyyy H:m:s 'GMT'", // RFC 1123 (r, except it allows both 1 and 01 for date and time)
            "ddd, d MMM yyyy H:m:s 'UTC'", // RFC 1123, UTC
            "ddd, d MMM yyyy H:m:s", // RFC 1123, no zone - assume GMT
            "d MMM yyyy H:m:s 'GMT'", // RFC 1123, no day-of-week
            "d MMM yyyy H:m:s 'UTC'", // RFC 1123, UTC, no day-of-week
            "d MMM yyyy H:m:s", // RFC 1123, no day-of-week, no zone
            "ddd, d MMM yy H:m:s 'GMT'", // RFC 1123, short year
            "ddd, d MMM yy H:m:s 'UTC'", // RFC 1123, UTC, short year
            "ddd, d MMM yy H:m:s", // RFC 1123, short year, no zone
            "d MMM yy H:m:s 'GMT'", // RFC 1123, no day-of-week, short year
            "d MMM yy H:m:s 'UTC'", // RFC 1123, UTC, no day-of-week, short year
            "d MMM yy H:m:s", // RFC 1123, no day-of-week, short year, no zone

            "dddd, d'-'MMM'-'yy H:m:s 'GMT'", // RFC 850
            "dddd, d'-'MMM'-'yy H:m:s 'UTC'", // RFC 850, UTC
            "dddd, d'-'MMM'-'yy H:m:s zzz", // RFC 850, offset
            "dddd, d'-'MMM'-'yy H:m:s", // RFC 850 no zone
            "ddd MMM d H:m:s yyyy", // ANSI C's asctime() format

            "ddd, d MMM yyyy H:m:s zzz", // RFC 5322
            "ddd, d MMM yyyy H:m:s", // RFC 5322 no zone
            "d MMM yyyy H:m:s zzz", // RFC 5322 no day-of-week
            "d MMM yyyy H:m:s", // RFC 5322 no day-of-week, no zone
        };

        /// <summary>
        /// Attempts to parse a string containing a date and time, and possibly a time zone offset,
        /// in one of the formats specified in <see href="https://tools.ietf.org/html/rfc850">RFC850</see>,
        /// <see href="https://tools.ietf.org/html/rfc1123">RFC1123</see>,
        /// and <see href="https://tools.ietf.org/html/rfc5322">RFC5322</see>,
        /// or ANSI C's <see href="https://linux.die.net/man/3/asctime"><c>asctime()</c></see> format.
        /// </summary>
        /// <param name="str">The string to parse.</param>
        /// <param name="result">When this method returns <see langword="true"/>,
        /// a <see cref="DateTimeOffset"/> representing the parsed date, time, and time zone offset.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if <paramref name="str"/> was successfully parsed;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string str, out DateTimeOffset result) =>
            DateTimeOffset.TryParseExact(
                str,
                DateFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out result);

        /// <summary>
        /// Formats the specified <see cref="DateTimeOffset"/>
        /// according to <see href="https://tools.ietf.org/html/rfc1123">RFC1123</see>.
        /// </summary>
        /// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/> to format.</param>
        /// <returns>A string containing the formatted <paramref name="dateTimeOffset"/>.</returns>
        public static string Format(DateTimeOffset dateTimeOffset)
            => dateTimeOffset.ToUniversalTime().ToString("r", DateTimeFormatInfo.InvariantInfo);

        /// <summary>
        /// Formats the specified <see cref="DateTime"/>
        /// according to <see href="https://tools.ietf.org/html/rfc1123">RFC1123</see>.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to format.</param>
        /// <returns>A string containing the formatted <paramref name="dateTime"/>.</returns>
        public static string Format(DateTime dateTime)
            => dateTime.ToUniversalTime().ToString("r", DateTimeFormatInfo.InvariantInfo);
    }
}