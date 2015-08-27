namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

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
        public void GetInitialPartial()
        {
            const int maxLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            request.AddRange(0, maxLength);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                var ms = new MemoryStream();
                response.GetResponseStream().CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                var subset = new byte[maxLength + 1];
                var originalSet = TestHelper.GetBigData();
                Buffer.BlockCopy(originalSet, 0, subset, 0, maxLength +1);
                Assert.AreEqual(subset, data);
            }
        }

        [Test]
        public void GetMiddlePartial()
        {
            const int offset = 50;
            const int maxLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            request.AddRange(offset, maxLength + offset);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                var ms = new MemoryStream();
                response.GetResponseStream().CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                var subset = new byte[maxLength + 1];
                var originalSet = TestHelper.GetBigData();
                Buffer.BlockCopy(originalSet, offset, subset, 0, maxLength + 1);
                Assert.AreEqual(subset, data);
            }
        }

        [Test]
        public void GetEntireFileWithChunks()
        {
            var originalSet = TestHelper.GetBigData();
            var requestHead = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            requestHead.Method = "HEAD";

            var remoteSize = ((HttpWebResponse) requestHead.GetResponse()).ContentLength;
            Assert.AreEqual(remoteSize, originalSet.Length);

            var buffer = new byte[remoteSize];
            const int chunkSize = 50000;

            for (var i = 0; i < remoteSize/chunkSize + 1; i++)
            {
                var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
                var top = ((i + 1)*chunkSize) - 1;

                request.AddRange(i*chunkSize, top > remoteSize ? remoteSize : top);

                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                    var ms = new MemoryStream();
                    response.GetResponseStream().CopyTo(ms);
                    var data = ms.ToArray();
                    Buffer.BlockCopy(data, 0, buffer, i * chunkSize, data.Length);
                }
            }

            Assert.AreEqual(originalSet, buffer);
        }

        [Test]
        public void GetNotPartial()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.SmallDataFile);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var ms = new MemoryStream();
                response.GetResponseStream().CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                Assert.AreEqual(TestHelper.GetSmallData(), data);
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
