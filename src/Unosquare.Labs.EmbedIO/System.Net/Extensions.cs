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

    /// <summary>
    /// Represents an asynchronous operation result.
    /// </summary>
    /// <seealso cref="System.IAsyncResult" />
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

        /// <summary>
        /// Completes the specified result synchronously.
        /// </summary>
        /// <param name="data">The data.</param>
        public void Complete(object data)
        {
            CompletedSynchronously = true;
            Data = data;
        }

        /// <summary>
        /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
        /// </summary>
        public object AsyncState { get; }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle" /> that is used to wait for an asynchronous operation to complete.
        /// </summary>
        public WaitHandle AsyncWaitHandle => null;

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation completed synchronously.
        /// </summary>
        public bool CompletedSynchronously { get; private set; }

        /// <summary>
        /// Gets the associated data of this async result.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public object Data { get; internal set; }

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted => CompletedSynchronously;
    }

    /// <summary>
    /// Extension MEthods for System.Net
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Begins and asynchronous read of the specified stream
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Retrieve the result of an asynchronous read for the specified stream
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="ares">The ares.</param>
        /// <returns></returns>
        public static int EndRead(this Stream stream, IAsyncResult ares)
        {
            var result = (AsyncResult)ares;
            return (int)result.Data;
        }

        /// <summary>
        /// Gets the byte array contents of a memory stream
        /// </summary>
        /// <param name="ms">The ms.</param>
        /// <returns></returns>
        public static byte[] GetBuffer(this MemoryStream ms)
        {
            return ms.ToArray();
        }

        /// <summary>
        /// Sets the value for a header KVP
        /// </summary>
        /// <param name="coll">The coll.</param>
        /// <param name="key">The key.</param>
        /// <param name="data">The data.</param>
        public static void SetInternal(this WebHeaderCollection coll, string key, string data)
        {
            coll[key] = data;
        }

        /// <summary>
        /// Parses and adds the data from a string into the specified Name-Value collection
        /// </summary>
        /// <param name="coll">The coll.</param>
        /// <param name="data">The data.</param>
        public static void Add(this NameValueCollection coll, string data)
        {
            var set = data.Split(':');
            if (set.Length == 2)
                coll[set[0].Trim()] = set[1].Trim();
        }

        /// <summary>
        /// Parses and adds the data from a string into the specified Name-Value collection
        /// </summary>
        /// <param name="coll">The coll.</param>
        /// <param name="data">The data.</param>
        public static void Add(this WebHeaderCollection coll, string data)
        {
            var set = data.Split(':');
            if (set.Length == 2)
                coll[set[0].Trim()] = set[1].Trim();
        }

        /// <summary>
        /// The scheme delimiter
        /// </summary>
        public static readonly string SchemeDelimiter = "://";
        /// <summary>
        /// The URI scheme file
        /// </summary>
        public static readonly string UriSchemeFile = "file";
        /// <summary>
        /// The URI scheme FTP
        /// </summary>
        public static readonly string UriSchemeFtp = "ftp";
        /// <summary>
        /// The URI scheme gopher
        /// </summary>
        public static readonly string UriSchemeGopher = "gopher";
        /// <summary>
        /// The URI scheme HTTP
        /// </summary>
        public static readonly string UriSchemeHttp = "http";
        /// <summary>
        /// The URI scheme HTTPS
        /// </summary>
        public static readonly string UriSchemeHttps = "https";
        /// <summary>
        /// The URI scheme mailto
        /// </summary>
        public static readonly string UriSchemeMailto = "mailto";
        /// <summary>
        /// The URI scheme news
        /// </summary>
        public static readonly string UriSchemeNews = "news";
        /// <summary>
        /// The URI scheme NNTP
        /// </summary>
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

        /// <summary>
        /// Gets the left part of the specified URI, inclusive of the specified Uri Partial.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="part">The part.</param>
        /// <returns></returns>
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