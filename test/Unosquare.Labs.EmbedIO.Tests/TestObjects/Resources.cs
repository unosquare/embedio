using System.IO;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public static class Resources
    {
        public const string ServerAddress = "http://localhost:7777/";
        public const string WsServerAddress = "ws://localhost:7777/";
        public static readonly string subIndex = File.ReadAllText("html/sub/index.html");
        public static readonly string index = File.ReadAllText("html/index.html");
    }
}
