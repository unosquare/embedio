using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EmbedIO.Testing
{
    public static class Resource
    {
        public static readonly string Prefix = typeof(Resource).Namespace + ".Resources";

        private static readonly Assembly Assembly;

        static Resource()
        {
            Assembly = Assembly.GetExecutingAssembly();
        }

        public static bool Exists(string path)
            => Assembly.GetManifestResourceNames().Contains(ConvertPath(path));

        public static bool TryOpen(string path, out Stream stream)
        {
            stream = Assembly.GetManifestResourceStream(ConvertPath(path));
            return stream != null;
        }

        public static Stream Open(string path)
            => Assembly.GetManifestResourceStream(ConvertPath(path));

        public static long GetLength(string path)
        {
            using (var stream = Open(path))
            {
                return stream.Length;
            }
        }

        public static byte[] GetBytes(string path)
        {
            using (var stream = Open(path))
            {
                var length = (int) stream.Length;
                if (length == 0)
                    return Array.Empty<byte>();

                var buffer = new byte[length];
                stream.Read(buffer, 0, length);
                return buffer;
            }
        }

        public static byte[] GetByteRange(string path, int start, int upperBound)
        {
            using (var stream = Open(path))
            {
                var length = (int) stream.Length;
                if (start >= length || upperBound < start || upperBound >= length)
                    return null;

                var rangeLength = upperBound - start + 1;
                var buffer = new byte[rangeLength];
                stream.Position = start;
                stream.Read(buffer, 0, rangeLength);
                return buffer;
            }
        }

        public static string GetText(string path, Encoding encoding = null)
        {
            using (var stream = Open(path))
            using (var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, false, WebServer.StreamCopyBufferSize, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static string ConvertPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path[0] == '/')
                path = path.Substring(1);

            return Prefix + path.Replace('/', '.');
        }
    }
}