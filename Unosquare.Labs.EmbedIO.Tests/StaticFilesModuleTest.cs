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
        const string HeaderPragmaValue = "no-cache";

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

                Assert.AreEqual(Resources.index, html, "Same content index.html");

                Assert.IsNullOrEmpty(response.Headers[Constants.HeaderPragma], "Pragma empty");
            }
            
            WebServer.Module<StaticFilesModule>().DefaultHeaders.Add(Constants.HeaderPragma, HeaderPragmaValue);

            request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                Assert.AreEqual(HeaderPragmaValue, response.Headers[Constants.HeaderPragma]);
            }
        }

        [Test]
        public async void GetSubFolderIndex()
        {
            var webClient = new WebClient();

            var html = await webClient.DownloadStringTaskAsync(Resources.ServerAddress + "sub/");

            Assert.AreEqual(html, Resources.subIndex, "Same content index.html");
            
            html = await webClient.DownloadStringTaskAsync(Resources.ServerAddress + "sub");

            Assert.AreEqual(html, Resources.subIndex, "Same content index.html without trailing");
        }

        [Test]
        public void GetEtag()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);
            string eTag;

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
                return;
            }

            Assert.Fail("The Exception should raise");
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
                var subset = new byte[maxLength];
                var originalSet = TestHelper.GetBigData();
                Buffer.BlockCopy(originalSet, 0, subset, 0, maxLength);
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
                var subset = new byte[maxLength];
                var originalSet = TestHelper.GetBigData();
                Buffer.BlockCopy(originalSet, offset, subset, 0, maxLength);
                Assert.AreEqual(subset, data);
            }
        }

        [Test]
        public void GetLastPart()
        {
            const int startByteIndex = 100;
            const int byteLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            request.AddRange(startByteIndex, startByteIndex + byteLength);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                var ms = new MemoryStream();
                response.GetResponseStream().CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                var subset = new byte[byteLength];
                var originalSet = TestHelper.GetBigData();
                Buffer.BlockCopy(originalSet, startByteIndex, subset, 0, byteLength);
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
                var top = (i + 1)*chunkSize;

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
        public void GetInvalidChunk()
        {
            var originalSet = TestHelper.GetBigData();
            var requestHead = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            requestHead.Method = "HEAD";

            var remoteSize = ((HttpWebResponse)requestHead.GetResponse()).ContentLength;
            Assert.AreEqual(remoteSize, originalSet.Length);

            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);
            request.AddRange(0, remoteSize + 10);

            try
            {
                request.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null || ex.Status != WebExceptionStatus.ProtocolError)
                    throw;

                var response = (HttpWebResponse)ex.Response;

                Assert.AreEqual(response.StatusCode, HttpStatusCode.RequestedRangeNotSatisfiable, "Status Code RequestedRangeNotSatisfiable");
                Assert.AreEqual(response.Headers["Content-Range"],
                    string.Format("bytes */{0}", remoteSize));
                return;
            }

            Assert.Fail("The Exception should raise");
        }

        [Test]
        public void GetNotPartial()
        {
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + "/" + TestHelper.BigDataFile);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var ms = new MemoryStream();
                response.GetResponseStream().CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                Assert.AreEqual(TestHelper.GetBigData(), data);
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