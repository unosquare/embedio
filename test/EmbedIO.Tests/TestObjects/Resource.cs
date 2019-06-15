using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EmbedIO.Tests.TestObjects
{
    public static class Resource
    {
        public const string Prefix = "EmbedIO.Tests.Resources.";

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