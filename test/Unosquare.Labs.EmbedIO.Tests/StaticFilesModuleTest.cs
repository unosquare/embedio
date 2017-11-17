namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using System.Linq;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

    [TestFixture]
    public class StaticFilesModuleTest : FixtureBase
    {
        private const string HeaderPragmaValue = "no-cache";

        public StaticFilesModuleTest()
            : base((ws) =>
            {
                ws.RegisterModule(new StaticFilesModule(TestHelper.SetupStaticFolder()) { UseRamCache = true });
                ws.RegisterModule(new FallbackModule("/index.html"));
            }, RoutingStrategy.Wildcard)
        {
        }

        public class GetFiles : StaticFilesModuleTest
        {
            [Test]
            public async Task Index()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        var html = await response.Content.ReadAsStringAsync();

                        Assert.AreEqual(Resources.Index, html, "Same content index.html");

                        Assert.IsTrue(string.IsNullOrWhiteSpace(response.Headers.Pragma.ToString()), "Pragma empty");
                    }

                    _webServer.Module<StaticFilesModule>().DefaultHeaders.Add(Headers.Pragma, HeaderPragmaValue);

                    request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                        Assert.AreEqual(HeaderPragmaValue, response.Headers.Pragma.ToString());
                    }
                }
            }

            [Test]
            public async Task SubFolderIndex()
            {
                var html = await GetString("sub/");

                Assert.AreEqual(Resources.SubIndex, html, "Same content index.html");

                html = await GetString("sub");

                Assert.AreEqual(Resources.SubIndex, html, "Same content index.html without trailing");
            }

            [Test]
            public async Task FallbackIndex()
            {
                var html = await GetString("invalidpath");

                Assert.AreEqual(Resources.Index, html, "Same content index.html");
            }

            [Test]
            public async Task TestHeadIndex()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, WebServerUrl);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        var html = await response.Content.ReadAsStringAsync();

                        Assert.IsEmpty(html, "Content Empty");
                    }
                }
            }

            [Test]
            public async Task FileWritable()
            {
                var endpoint = Resources.GetServerAddress();
                var root = Path.GetTempPath();
                var file = Path.Combine(root, "index.html");
                File.WriteAllText(file, Resources.Index);

                using (var server = new WebServer(endpoint))
                {
                    server.RegisterModule(new StaticFilesModule(root) { UseRamCache = false });
                    var runTask = server.RunAsync();
                    using (var webClient = new HttpClient())
                    {
                        var remoteFile = await webClient.GetStringAsync(endpoint);
                        File.WriteAllText(file, Resources.SubIndex);

                        var remoteUpdatedFile = await webClient.GetStringAsync(endpoint);
                        File.WriteAllText(file, nameof(WebServer));

                        Assert.AreEqual(Resources.Index, remoteFile);
                        Assert.AreEqual(Resources.SubIndex, remoteUpdatedFile);
                    }
                }
            }

            [Test]
            public async Task SensitiveFile()
            {
                var file = Path.GetTempPath() + Guid.NewGuid().ToString().ToLower();
                File.WriteAllText(file, "");

                Assert.IsTrue(File.Exists(file), "File was created");

                if (File.Exists(file.ToUpper()))
                {
                    Assert.Ignore("File-system is not case sensitive. Ignoring");
                }
                else
                {
                    var htmlUpperCase = await GetString(TestHelper.UppercaseFile);

                    Assert.AreEqual(nameof(TestHelper.UppercaseFile), htmlUpperCase, "Same content upper case");

                    var htmlLowerCase = await GetString(TestHelper.LowercaseFile);

                    Assert.AreEqual(nameof(TestHelper.LowercaseFile), htmlLowerCase, "Same content lower case");
                }
            }
        }

        public class GetPartials : StaticFilesModuleTest
        {
            [Test]
            public async Task Initial()
            {
                using (var client = new HttpClient())
                {
                    const int maxLength = 100;
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, maxLength - 1);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                        using (var ms = new MemoryStream())
                        {
                            var responseStream = await response.Content.ReadAsStreamAsync();
                            responseStream.CopyTo(ms);
                            var data = ms.ToArray();

                            Assert.IsNotNull(data, "Data is not empty");
                            var subset = new byte[maxLength];
                            var originalSet = TestHelper.GetBigData();
                            Buffer.BlockCopy(originalSet, 0, subset, 0, maxLength);
                            Assert.IsTrue(subset.SequenceEqual(data));
                        }
                    }
                }                    
            }

            [Test]
            public async Task Middle()
            {
                using(var client = new HttpClient())
                {
                    const int offset = 50;
                    const int maxLength = 100;
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, maxLength + offset - 1);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                        using (var ms = new MemoryStream())
                        {
                            var responseStream = await response.Content.ReadAsStreamAsync();
                            responseStream.CopyTo(ms);
                            var data = ms.ToArray();

                            Assert.IsNotNull(data, "Data is not empty");
                            var subset = new byte[maxLength];
                            var originalSet = TestHelper.GetBigData();
                            Buffer.BlockCopy(originalSet, offset, subset, 0, maxLength);
                            Assert.IsTrue(subset.SequenceEqual(data));
                        }
                    }
                }
            }

            [Test]
            public async Task NotPartial()
            {
                using(var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        using (var ms = new MemoryStream())
                        {
                            var responseStream = await response.Content.ReadAsStreamAsync();
                            responseStream.CopyTo(ms);
                            var data = ms.ToArray();

                            Assert.IsNotNull(data, "Data is not empty");
                            Assert.IsTrue(TestHelper.GetBigData().SequenceEqual(data));
                        }
                    }
                }
            }
        }

        public class GetChunks : StaticFilesModuleTest
        {
            [Test]
            public async Task GetEntireFileWithChunksUsingRange()
            {
                var originalSet = TestHelper.GetBigData();
                var requestHead = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestHelper.BigDataFile);
                requestHead.Method = "HEAD";

                var remoteSize = ((HttpWebResponse)requestHead.GetResponse()).ContentLength;
                Assert.AreEqual(remoteSize, originalSet.Length);

                var buffer = new byte[remoteSize];
                const int chunkSize = 100000;

                for (var i = 0; i < remoteSize / chunkSize + 1; i++)
                {
                    var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestHelper.BigDataFile);
                    var top = (i + 1) * chunkSize;

                    request.AddRange(i * chunkSize, (top > remoteSize ? remoteSize : top) - 1);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        if (remoteSize < top)
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

                        using (var ms = new MemoryStream())
                        {
                            response.GetResponseStream()?.CopyTo(ms);
                            var data = ms.ToArray();
                            Buffer.BlockCopy(data, 0, buffer, i * chunkSize, data.Length);
                        }
                    }
                }

                Assert.IsTrue(originalSet.SequenceEqual(buffer));
            }

            [Test]
            public async Task GetInvalidChunk()
            {
                var originalSet = TestHelper.GetBigData();
                var requestHead = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestHelper.BigDataFile);
                requestHead.Method = "HEAD";

                var remoteSize = ((HttpWebResponse)await requestHead.GetResponseAsync()).ContentLength;
                Assert.AreEqual(remoteSize, originalSet.Length);

                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestHelper.BigDataFile);
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
        }

        public class CompressFile : StaticFilesModuleTest
        {
            [Test]
            public async Task GetGzip()
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
                    var reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    var html = reader.ReadToEnd();

                    Assert.IsNotNull(html, "Data is not empty");
                    Assert.AreEqual(Resources.Index, html);
                }
            }
        }

        public class FileParts : StaticFilesModuleTest
        {
            [Test]
            public void GetLastPart()
            {
                const int startByteIndex = 100;
                const int byteLength = 100;
                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestHelper.BigDataFile);
                request.AddRange(startByteIndex, startByteIndex + byteLength - 1);

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
                        Assert.IsTrue(subset.SequenceEqual(data));
                    }
                }
            }
        }

        public class Etag : StaticFilesModuleTest
        {
            [Test]
            public async Task GetEtag()
            {
                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
                string eTag;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                    Assert.NotNull(response.Headers[Headers.ETag], "ETag is not null");
                    eTag = response.Headers[Headers.ETag];
                }

                var secondRequest = (HttpWebRequest)WebRequest.Create(WebServerUrl);
                secondRequest.Headers[Headers.IfNotMatch] = eTag;

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
        }
    }
}