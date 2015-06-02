using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
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
            RootPath = TestHelper.SetupStaticFolder();

            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RegisterModule(new StaticFilesModule(RootPath) { UseRamCache = true });
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
                Assert.NotNull(response.Headers[EmbedIO.Constants.HeaderETag], "ETag is not null");
                eTag = response.Headers[EmbedIO.Constants.HeaderETag];
            }

            var secondRequest = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            secondRequest.Headers.Add(EmbedIO.Constants.HeaderIfNotMatch, eTag);

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
                Assert.IsTrue(Resources.index.StartsWith(html), "Content starts at index.html");
            }
        }

        [Test]
        public void HeadIndex()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            request.Method = HttpVerbs.Head.ToString();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var html = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNullOrEmpty(html, "Content Empty");
            }
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}
