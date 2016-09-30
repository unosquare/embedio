#if !NET452
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum UriPartial
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Scheme,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Authority,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Path,
        /// <devdoc>
        ///    <para> Denotes a left part of a uri up to and including the query </para>
        /// </devdoc>
        Query
    }

    public class AsyncResult : IAsyncResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncResult"/> class.
        /// </summary>
        /// <param name="state">The state.</param>
        public AsyncResult(object state)
        {
            AsyncState = state;
        }

        public void Complete(object data)
        {
            CompletedSynchronously = true;
            Data = data;
        }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle => null;

        public bool CompletedSynchronously { get; private set; }

        public object Data { get; internal set; }

        public bool IsCompleted => CompletedSynchronously;
    }

    public static class Extensions
    {
        public static IAsyncResult BeginRead(this Stream stream, byte[] buffer,
    int offset,
    int count,
    AsyncCallback callback,
    object state)
        {
            var result = new AsyncResult(state);

            Task.Run(() =>
            {
                try
                {
                    var data = stream.Read(buffer, offset, count);
                    result.Complete(data);
                    callback(result);
                }
                catch (IOException)
                {
                    // Ignore, possible connection closed
                }
            });

            return result;
        }

        public static int EndRead(this Stream stream, IAsyncResult ares)
        {
            var result = (AsyncResult)ares;
            return (int)result.Data;
        }

        public static byte[] GetBuffer(this MemoryStream ms)
        {
            return ms.ToArray();
        }

        public static void SetInternal(this WebHeaderCollection coll, string key, string data)
        {
            coll[key] = data;
        }

        public static void Add(this NameValueCollection coll, string data)
        {
            var set = data.Split(':');
            if (set.Length == 2)
                coll[set[0].Trim()] = set[1].Trim();
        }

        public static void Add(this WebHeaderCollection coll, string data)
        {
            var set = data.Split(':');
            if (set.Length == 2)
                coll[set[0].Trim()] = set[1].Trim();
        }

        public static readonly string SchemeDelimiter = "://";
        public static readonly string UriSchemeFile = "file";
        public static readonly string UriSchemeFtp = "ftp";
        public static readonly string UriSchemeGopher = "gopher";
        public static readonly string UriSchemeHttp = "http";
        public static readonly string UriSchemeHttps = "https";
        public static readonly string UriSchemeMailto = "mailto";
        public static readonly string UriSchemeNews = "news";
        public static readonly string UriSchemeNntp = "nntp";

        private struct UriScheme
        {
            public readonly string Scheme;
            public readonly string Delimiter;
            public readonly int DefaultPort;

            public UriScheme(string s, string d, int p)
            {
                Scheme = s;
                Delimiter = d;
                DefaultPort = p;
            }
        };

        static readonly UriScheme[] _schemes = {
            new UriScheme (UriSchemeHttp, SchemeDelimiter, 80),
            new UriScheme (UriSchemeHttps, SchemeDelimiter, 443),
            new UriScheme (UriSchemeFtp, SchemeDelimiter, 21),
            new UriScheme (UriSchemeFile, SchemeDelimiter, -1),
            new UriScheme (UriSchemeMailto, ":", 25),
            new UriScheme (UriSchemeNews, ":", -1),
            new UriScheme (UriSchemeNntp, SchemeDelimiter, 119),
            new UriScheme (UriSchemeGopher, SchemeDelimiter, 70)
        };

        internal static string GetSchemeDelimiter(string scheme)
        {
            for (var i = 0; i < _schemes.Length; i++)
                if (_schemes[i].Scheme == scheme)
                    return _schemes[i].Delimiter;
            return SchemeDelimiter;
        }

        internal static int GetDefaultPort(string scheme)
        {
            for (var i = 0; i < _schemes.Length; i++)
                if (_schemes[i].Scheme == scheme)
                    return _schemes[i].DefaultPort;
            return -1;
        }

        private static string GetOpaqueWiseSchemeDelimiter(string scheme, bool isOpaquePart = false)
        {
            if (isOpaquePart)
                return ":";
            return GetSchemeDelimiter(scheme);
        }

        public static string GetLeftPart(this Uri uri, UriPartial part)
        {
            int defaultPort;
            switch (part)
            {
                case UriPartial.Scheme:
                    return uri.Scheme + GetOpaqueWiseSchemeDelimiter(uri.Scheme);
                case UriPartial.Authority:
                    if (uri.Host == string.Empty ||
                        uri.Scheme == UriSchemeMailto ||
                        uri.Scheme == UriSchemeNews)
                        return string.Empty;

                    var s = new StringBuilder();
                    s.Append(uri.Scheme);
                    s.Append(GetOpaqueWiseSchemeDelimiter(uri.Scheme));
                    if (uri.AbsolutePath.Length > 1 && uri.AbsolutePath[1] == ':' && (UriSchemeFile == uri.Scheme))
                        s.Append('/');  // win32 file
                    if (uri.UserInfo.Length > 0)
                        s.Append(uri.UserInfo).Append('@');
                    s.Append(uri.Host);
                    defaultPort = GetDefaultPort(uri.Scheme);
                    if ((uri.Port != -1) && (uri.Port != defaultPort))
                        s.Append(':').Append(uri.Port);
                    return s.ToString();
                case UriPartial.Path:
                    var sb = new StringBuilder();
                    sb.Append(uri.Scheme);
                    sb.Append(GetOpaqueWiseSchemeDelimiter(uri.Scheme));
                    if (uri.AbsolutePath.Length > 1 && uri.AbsolutePath[1] == ':' && (UriSchemeFile == uri.Scheme))
                        sb.Append('/');  // win32 file
                    if (uri.UserInfo.Length > 0)
                        sb.Append(uri.UserInfo).Append('@');
                    sb.Append(uri.Host);
                    defaultPort = GetDefaultPort(uri.Scheme);
                    if ((uri.Port != -1) && (uri.Port != defaultPort))
                        sb.Append(':').Append(uri.Port);
                    sb.Append(uri.AbsolutePath);
                    return sb.ToString();
            }
            return null;
        }
    }
}
#endif