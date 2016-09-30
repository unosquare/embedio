using System.IO;
using System.Reflection;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public static class Resources
    {
        public const string ServerAddress = "http://localhost:7777/";
        public const string WsServerAddress = "ws://localhost:7777/";

        public static readonly string CurrentPath =
            Path.GetDirectoryName(typeof (Resources).GetTypeInfo().Assembly.Location);

        public static readonly string SubIndex = File.ReadAllText(Path.Combine(CurrentPath, "html/sub/index.html"));
        public static readonly string Index = File.ReadAllText(Path.Combine(CurrentPath, "html/index.html"));
    }
}
