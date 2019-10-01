using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using EmbedIO.Internal;
using EmbedIO.Net.Internal;
using EmbedIO.Utilities;

namespace EmbedIO.Net
{
    /// <summary>
    /// <para>Provides a collection container for instances of <see cref="Cookie"/>.</para>
    /// <para>This class is meant to be used internally by EmbedIO; you don't need to
    /// use this class directly.</para>
    /// </summary>
#pragma warning disable CA1710 // Rename class to end in 'Collection' - it ends in 'List', i.e. 'Indexed Collection'.
    public sealed class CookieList : List<Cookie>, ICookieCollection
#pragma warning restore CA1710
    {
        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        public Cookie? this[string name]
        {
            get
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                if (Count == 0)
                    return null;

                var list = new List<Cookie>(this);

                list.Sort(CompareCookieWithinSorted);

                return list.FirstOrDefault(cookie => cookie.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>Creates a <see cref="CookieList"/> by parsing
        /// the value of one or more <c>Cookie</c> or <c>Set-Cookie</c> headers.</summary>
        /// <param name="headerValue">The value, or comma-separated list of values,
        /// of the header or headers.</param>
        /// <returns>A newly-created instance of <see cref="CookieList"/>.</returns>
        public static CookieList Parse(string headerValue)
        {
            var cookies = new CookieList();

            Cookie? cookie = null;
            var pairs = SplitCookieHeaderValue(headerValue);

            for (var i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i].Trim();
                if (pair.Length == 0)
                    continue;

                if (pair.StartsWith("version", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Version = int.Parse(GetValue(pair, true), CultureInfo.InvariantCulture);
                }
                else if (pair.StartsWith("expires", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    var buff = new StringBuilder(GetValue(pair), 32);
                    if (i < pairs.Length - 1)
                        buff.AppendFormat(CultureInfo.InvariantCulture, ", {0}", pairs[++i].Trim());

                    if (!HttpDate.TryParse(buff.ToString(), out var expires))
                        expires = DateTimeOffset.Now;

                    if (cookie.Expires == DateTime.MinValue)
                        cookie.Expires = expires.LocalDateTime;
                }
                else if (pair.StartsWith("max-age", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    var max = int.Parse(GetValue(pair, true), CultureInfo.InvariantCulture);

                    cookie.Expires = DateTime.Now.AddSeconds(max);
                }
                else if (pair.StartsWith("path", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Path = GetValue(pair);
                }
                else if (pair.StartsWith("domain", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Domain = GetValue(pair);
                }
                else if (pair.StartsWith("port", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Port = pair.Equals("port", StringComparison.OrdinalIgnoreCase)
                        ? "\"\""
                        : GetValue(pair);
                }
                else if (pair.StartsWith("comment", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Comment = WebUtility.UrlDecode(GetValue(pair));
                }
                else if (pair.StartsWith("commenturl", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.CommentUri = UriUtility.StringToUri(GetValue(pair, true));
                }
                else if (pair.StartsWith("discard", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Discard = true;
                }
                else if (pair.StartsWith("secure", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.Secure = true;
                }
                else if (pair.StartsWith("httponly", StringComparison.OrdinalIgnoreCase) && cookie != null)
                {
                    cookie.HttpOnly = true;
                }
                else
                {
                    if (cookie != null)
                        cookies.Add(cookie);

                    cookie = ParseCookie(pair);
                }
            }

            if (cookie != null)
                cookies.Add(cookie);

            return cookies;
        }

        /// <inheritdoc />
        public new void Add(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException(nameof(cookie));

            var pos = SearchCookie(cookie);
            if (pos == -1)
            {
                base.Add(cookie);
                return;
            }

            this[pos] = cookie;
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Less than zero.");

            if (array.Rank > 1)
                throw new ArgumentException("Multidimensional.", nameof(array));

            if (array.Length - index < Count)
            {
                throw new ArgumentException(
                    "The number of elements in this collection is greater than the available space of the destination array.");
            }

            if (array.GetType().GetElementType()?.IsAssignableFrom(typeof(Cookie)) != true)
            {
                throw new InvalidCastException(
                    "The elements in this collection cannot be cast automatically to the type of the destination array.");
            }

            ((IList) this).CopyTo(array, index);
        }

        private static string? GetValue(string nameAndValue, bool unquote = false)
        {
            var idx = nameAndValue.IndexOf('=');

            if (idx < 0 || idx == nameAndValue.Length - 1)
                return null;

            var val = nameAndValue.Substring(idx + 1).Trim();
            return unquote ? val.Unquote() : val;
        }

        private static string[] SplitCookieHeaderValue(string value) => value.SplitHeaderValue(true).ToArray();

        private static int CompareCookieWithinSorted(Cookie x, Cookie y)
        {
            var ret = x.Version - y.Version;
            return ret != 0
                ? ret
                : (ret = string.Compare(x.Name, y.Name, StringComparison.Ordinal)) != 0
                    ? ret
                    : y.Path.Length - x.Path.Length;
        }

        private static Cookie ParseCookie(string pair)
        {
            string name;
            var val = string.Empty;

            var pos = pair.IndexOf('=');
            if (pos == -1)
            {
                name = pair;
            }
            else if (pos == pair.Length - 1)
            {
                name = pair.Substring(0, pos).TrimEnd(' ');
            }
            else
            {
                name = pair.Substring(0, pos).TrimEnd(' ');
                val = pair.Substring(pos + 1).TrimStart(' ');
            }

            return new Cookie(name, val);
        }

        private int SearchCookie(Cookie cookie)
        {
            var name = cookie.Name;
            var path = cookie.Path;
            var domain = cookie.Domain;
            var ver = cookie.Version;

            for (var i = Count - 1; i >= 0; i--)
            {
                var c = this[i];
                if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    c.Path.Equals(path, StringComparison.OrdinalIgnoreCase) &&
                    c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase) &&
                    c.Version == ver)
                    return i;
            }

            return -1;
        }
    }
}