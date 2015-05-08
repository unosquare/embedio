using System.Net;
using Unosquare.Labs.EmbedIO.Tests.Properties;

namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.IO;
    using Unosquare.Labs.EmbedIO.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class StaticFilesModuleTest
    {
        private const string ServerAddress = "http://localhost:7777/";
        protected string RootPath;

        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            var assemblyPath = Path.GetDirectoryName(typeof (StaticFilesModuleTest).Assembly.Location);
            RootPath = Path.Combine(assemblyPath, "html");

            if (Directory.Exists(RootPath) == false)
                Directory.CreateDirectory(RootPath);

            if (File.Exists(Path.Combine(RootPath, "index.html")) == false)
                File.WriteAllText(Path.Combine(RootPath, "index.html"), Resources.index);

            WebServer = new WebServer(ServerAddress, Logger);
            WebServer.RegisterModule(new StaticFilesModule(RootPath));
            WebServer.RunAsync();
        }

        [Test]
        public void GetIndex()
        {
            var request = (HttpWebRequest)WebRequest.Create(ServerAddress);
            var response = (HttpWebResponse)request.GetResponse();

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
        }
    }
}
