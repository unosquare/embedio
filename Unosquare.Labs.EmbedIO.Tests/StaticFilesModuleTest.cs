namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.IO;
    using System.Net;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class StaticFilesModuleTest
    {
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

            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RegisterModule(new StaticFilesModule(RootPath));
            WebServer.RunAsync();
        }

        [Test]
        public void GetIndex()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var html = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(html, Resources.index, "Same content index.html");
            }
        }

        [Test]
        public void GetEtag()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            var eTag = "";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                Assert.NotNull(response.Headers[EmbedIO.Extensions.HeaderETag], "ETag is not null");
                eTag = response.Headers[EmbedIO.Extensions.HeaderETag];
            }

            var secondRequest = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            secondRequest.Headers.Add(EmbedIO.Extensions.HeaderIfNotMatch, eTag);

            try
            {
                // By design GetResponse throws exception with NotModified status, weird
                secondRequest.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null || ex.Status != WebExceptionStatus.ProtocolError)
                    throw;

                var response = (HttpWebResponse)ex.Response;

                Assert.AreEqual(response.StatusCode, HttpStatusCode.NotModified, "Status Code NotModified");
            }
        }

        [Test]
        public void GetPartial()
        {
            const int maxLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            request.AddRange(0, maxLength);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                var html = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Assert.IsNotNullOrEmpty(html, "HTML is not empty");

                // Remove carryline before
                var sub = Resources.index.Substring(0, maxLength + 1).Replace("\r\n", "");
                html = html.Replace("\r\n", "");

                Assert.AreEqual(sub, html, "Same content index.html");
            }
        }

        [TearDown]
        public void Kill()
        {
            WebServer.Dispose();
        }
    }
}
