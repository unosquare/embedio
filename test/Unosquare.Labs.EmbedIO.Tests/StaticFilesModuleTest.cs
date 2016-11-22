namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Net.Http;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Modules;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;
    using System.IO.Compression;

    [TestFixture]
    public class StaticFilesModuleTest
    {
        const string HeaderPragmaValue = "no-cache";

        protected string RootPath;
        protected WebServer WebServer;
        
        protected string WebServerUrl;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            RootPath = TestHelper.SetupStaticFolder();

            WebServer = new WebServer(WebServerUrl, Logger);
            WebServer.RegisterModule(new StaticFilesModule(RootPath) { UseRamCache = true });
            WebServer.RunAsync();
        }

        [Test]
        public async Task GetIndex()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var html = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(Resources.Index, html, "Same content index.html");

                Assert.IsTrue(string.IsNullOrWhiteSpace(response.Headers[Constants.HeaderPragma]), "Pragma empty");
            }
            
            WebServer.Module<StaticFilesModule>().DefaultHeaders.Add(Constants.HeaderPragma, HeaderPragmaValue);

            request = (HttpWebRequest)WebRequest.Create(WebServerUrl);

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                Assert.AreEqual(HeaderPragmaValue, response.Headers[Constants.HeaderPragma]);
            }
        }

        [Test]
        public async Task GetSubFolderIndex()
        {
            var webClient = new HttpClient();

            var html = await webClient.GetStringAsync(WebServerUrl + "sub/");

            Assert.AreEqual(Resources.SubIndex, html, "Same content index.html");

            html = await webClient.GetStringAsync(WebServerUrl + "sub");

            Assert.AreEqual(Resources.SubIndex, html, "Same content index.html without trailing");
        }

        [Test]
        public async Task GetEtag()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            string eTag;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                Assert.NotNull(response.Headers[EmbedIO.Constants.HeaderETag], "ETag is not null");
                eTag = response.Headers[EmbedIO.Constants.HeaderETag];
            }

            var secondRequest = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            secondRequest.Headers[Constants.HeaderIfNotMatch] = eTag;

            try
            {
                // By design GetResponse throws exception with NotModified status, weird
                await secondRequest.GetResponseAsync();
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

#if !NETCOREAPP1_0
        [Test]
        public void GetInitialPartial()
        {
            const int maxLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            request.AddRange(0, maxLength);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream()?.CopyTo(ms);
                    var data = ms.ToArray();

                    Assert.IsNotNull(data, "Data is not empty");
                    var subset = new byte[maxLength];
                    var originalSet = TestHelper.GetBigData();
                    Buffer.BlockCopy(originalSet, 0, subset, 0, maxLength);
                    Assert.AreEqual(subset, data);
                }
            }
        }

        [Test]
        public void GetMiddlePartial()
        {
            const int offset = 50;
            const int maxLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            request.AddRange(offset, maxLength + offset);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream()?.CopyTo(ms);
                    var data = ms.ToArray();

                    Assert.IsNotNull(data, "Data is not empty");
                    var subset = new byte[maxLength];
                    var originalSet = TestHelper.GetBigData();
                    Buffer.BlockCopy(originalSet, offset, subset, 0, maxLength);
                    Assert.AreEqual(subset, data);
                }
            }
        }

        [Test]
        public void GetLastPart()
        {
            const int startByteIndex = 100;
            const int byteLength = 100;
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            request.AddRange(startByteIndex, startByteIndex + byteLength);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream()?.CopyTo(ms);
                    var data = ms.ToArray();

                    Assert.IsNotNull(data, "Data is not empty");
                    var subset = new byte[byteLength];
                    var originalSet = TestHelper.GetBigData();
                    Buffer.BlockCopy(originalSet, startByteIndex, subset, 0, byteLength);
                    Assert.AreEqual(subset, data);
                }
            }
        }

        [Test]
        public void GetEntireFileWithChunks()
        {
            var originalSet = TestHelper.GetBigData();
            var requestHead = (HttpWebRequest) WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            requestHead.Method = "HEAD";

            var remoteSize = ((HttpWebResponse) requestHead.GetResponse()).ContentLength;
            Assert.AreEqual(remoteSize, originalSet.Length);

            var buffer = new byte[remoteSize];
            const int chunkSize = 50000;

            for (var i = 0; i < remoteSize/chunkSize + 1; i++)
            {
                var request = (HttpWebRequest) WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
                var top = (i + 1)*chunkSize;

                request.AddRange(i*chunkSize, top > remoteSize ? remoteSize : top);

                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                    using (var ms = new MemoryStream())
                    {
                        response.GetResponseStream()?.CopyTo(ms);
                        var data = ms.ToArray();
                        Buffer.BlockCopy(data, 0, buffer, i*chunkSize, data.Length);
                    }
                }
            }

            Assert.AreEqual(originalSet, buffer);
        }

        [Test]
        public async Task GetInvalidChunk()
        {
            var originalSet = TestHelper.GetBigData();
            var requestHead = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            requestHead.Method = "HEAD";

            var remoteSize = ((HttpWebResponse)await requestHead.GetResponseAsync()).ContentLength;
            Assert.AreEqual(remoteSize, originalSet.Length);

            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);
            request.AddRange(0, remoteSize + 10);

            try
            {
                await request.GetResponseAsync();
            }
            catch (WebException ex)
            {
                if (ex.Response == null || ex.Status != WebExceptionStatus.ProtocolError)
                    throw;

                var response = (HttpWebResponse)ex.Response;

                Assert.AreEqual(response.StatusCode, HttpStatusCode.RequestedRangeNotSatisfiable, "Status Code RequestedRangeNotSatisfiable");
                Assert.AreEqual(response.Headers["Content-Range"],
                    $"bytes */{remoteSize}");
                return;
            }

            Assert.Fail("The Exception should raise");
        }
#endif

        [Test]
        public async Task GetNotPartial()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "/" + TestHelper.BigDataFile);

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream()?.CopyTo(ms);
                    var data = ms.ToArray();

                    Assert.IsNotNull(data, "Data is not empty");
                    Assert.AreEqual(TestHelper.GetBigData(), data);
                }
            }
        }

#if !NETCOREAPP1_0
        [Test]
        public async Task GetGzipCompressFile()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                
                // TODO: I need to fix this
                //Assert.IsTrue(response.ContentEncoding.ToLower().Contains("gzip"), "Request is gziped");
                //var responseStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                var responseStream = response.GetResponseStream();
                var reader =  new StreamReader(responseStream, Constants.DefaultEncoding);
                var html = reader.ReadToEnd();

                Assert.IsNotNull(html, "Data is not empty");
                Assert.AreEqual(Resources.Index, html);
            }
        }
#endif

        [Test]
        public async Task HeadIndex()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            request.Method = HttpVerbs.Head.ToString();

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var html = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsEmpty(html, "Content Empty");
            }
        }

        [Test]
        public async Task FileWritable()
        {
            var endpoint = Resources.GetServerAddress();
            var root = Path.GetTempPath();
            var file = Path.Combine(root, "index.html");
            File.WriteAllText(file, Resources.Index);

            using (var server = new WebServer(endpoint, Logger))
            {
                server.RegisterModule(new StaticFilesModule(root));
                var serverTask = server.RunAsync();

                var webClient = new HttpClient();
                var remoteFile = await webClient.GetStringAsync(endpoint);
                File.WriteAllText(file, Resources.SubIndex);
                await Task.Delay(100);
                var remoteUpdatedFile = await webClient.GetStringAsync(endpoint);
                File.WriteAllText(file, nameof(WebServer));

                Assert.AreEqual(Resources.Index, remoteFile);
                Assert.AreEqual(Resources.SubIndex, remoteUpdatedFile);
            }
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer?.Dispose();
        }
    }
}