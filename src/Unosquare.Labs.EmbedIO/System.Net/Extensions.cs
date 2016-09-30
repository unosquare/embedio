#if !NET452
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
        private bool _IsCompleted;
        private object _state;

        public AsyncResult(object state)
        {
            _state = state;
        }

        public void Complete(object data)
        {
            _IsCompleted = true;
            Data = data;
        }

        public object AsyncState
        {
            get
            {
                return _state;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return null;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return _IsCompleted;
            }
        }

        public object Data { get; internal set; }

        public bool IsCompleted
        {
            get
            {
                return _IsCompleted;
            }
        }
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
            public string scheme;
            public string delimiter;
            public int defaultPort;

            public UriScheme(string s, string d, int p)
            {
                scheme = s;
                delimiter = d;
                defaultPort = p;
            }
        };

        static UriScheme[] schemes = new UriScheme[] {
            new UriScheme (UriSchemeHttp, SchemeDelimiter, 80),
            new UriScheme (UriSchemeHttps, SchemeDelimiter, 443),
            new UriScheme (UriSchemeFtp, SchemeDelimiter, 21),
            new UriScheme (UriSchemeFile, SchemeDelimiter, -1),
            new UriScheme (UriSchemeMailto, ":", 25),
            new UriScheme (UriSchemeNews, ":", -1),
            new UriScheme (UriSchemeNntp, SchemeDelimiter, 119),
            new UriScheme (UriSchemeGopher, SchemeDelimiter, 70),
        };

        internal static string GetSchemeDelimiter(string scheme)
        {
            for (int i = 0; i < schemes.Length; i++)
                if (schemes[i].scheme == scheme)
                    return schemes[i].delimiter;
            return SchemeDelimiter;
        }

        internal static int GetDefaultPort(string scheme)
        {
            for (int i = 0; i < schemes.Length; i++)
                if (schemes[i].scheme == scheme)
                    return schemes[i].defaultPort;
            return -1;
        }

        private static string GetOpaqueWiseSchemeDelimiter(string scheme, bool isOpaquePart = false)
        {
            if (isOpaquePart)
                return ":";
            else
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
                    if (uri.Host == String.Empty ||
                        uri.Scheme == UriSchemeMailto ||
                        uri.Scheme == UriSchemeNews)
                        return String.Empty;

                    StringBuilder s = new StringBuilder();
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
                    StringBuilder sb = new StringBuilder();
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