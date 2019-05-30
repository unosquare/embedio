using EmbedIO.Files;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class StaticFilesModuleTest : FixtureBase
    {
        private const string HeaderPragmaValue = "no-cache";

        protected StaticFilesModuleTest()
            : base(ws => ws.WithStaticFolderAt("/", TestHelper.SetupStaticFolder()))
        {
        }
        
        private static async Task ValidatePayload(HttpResponseMessage response, int maxLength, int offset = 0)
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

                    WebServerInstance.Modules.OfType<StaticFilesModule>().First().DefaultHeaders
                        .Add(HttpHeaderNames.Pragma, HeaderPragmaValue);

                    request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                        Assert.AreEqual(HeaderPragmaValue, response.Headers.Pragma.ToString());
                    }
                }
            }

            [TestCase("sub/")]
            [TestCase("sub")]
            public async Task SubFolderIndex(string url)
            {
                var html = await GetString(url);

                Assert.AreEqual(Resources.SubIndex, html, $"Same content {url}");
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
                    server.Modules.Add(nameof(StaticFilesModule), new StaticFilesModule("/", root));
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
                File.WriteAllText(file, string.Empty);

                Assert.IsTrue(File.Exists(file), "File was created");

                if (File.Exists(file.ToUpper()))
                {
                    Assert.Ignore("File-system is not case sensitive. Ignoring");
                }

                var htmlUpperCase = await GetString(TestHelper.UppercaseFile);
                Assert.AreEqual(nameof(TestHelper.UppercaseFile), htmlUpperCase, "Same content upper case");

                var htmlLowerCase = await GetString(TestHelper.LowercaseFile);
                Assert.AreEqual(nameof(TestHelper.LowercaseFile), htmlLowerCase, "Same content lower case");
            }

            [Test]
            public void InvalidFilePath_ThrowsArgumentException()
            {
                Assert.Throws<ArgumentException>(() => new StaticFilesModule("/", "e:"));
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
                        await ValidatePayload(response, maxLength);
                    }
                }
            }

            [Test]
            public async Task Middle()
            {
                using (var client = new HttpClient())
                {
                    const int offset = 50;
                    const int maxLength = 100;
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                    request.Headers.Range =
                        new System.Net.Http.Headers.RangeHeaderValue(offset, maxLength + offset - 1);

                    using (var response = await client.SendAsync(request))
                    {
                        await ValidatePayload(response, maxLength, offset);
                    }
                }
            }

            [Test]
            public async Task GetLastPart()
            {
                using (var client = new HttpClient())
                {
                    const int offset = 100;
                    const int maxLength = 100;
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                    request.Headers.Range =
                        new System.Net.Http.Headers.RangeHeaderValue(offset, offset + maxLength - 1);

                    using (var response = await client.SendAsync(request))
                    {
                        await ValidatePayload(response, maxLength, offset);
                    }
                }
            }

            [Test]
            public async Task NotPartial()
            {
                using (var client = new HttpClient())
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
                using (var client = new HttpClient())
                {
                    var originalSet = TestHelper.GetBigData();
                    var requestHead = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);

                    using (var res = await client.SendAsync(requestHead))
                    {
                        var remoteSize = await res.Content.ReadAsByteArrayAsync();
                        Assert.AreEqual(remoteSize.Length, originalSet.Length);

                        var buffer = new byte[remoteSize.Length];
                        const int chunkSize = 100000;
                        for (var i = 0; i < (remoteSize.Length / chunkSize) + 1; i++)
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                            var top = (i + 1) * chunkSize;

                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(i * chunkSize,
                                (top > remoteSize.Length ? remoteSize.Length : top) - 1);

                            using (var response = await client.SendAsync(request))
                            {
                                if (remoteSize.Length < top)
                                {
                                    Assert.AreEqual(
                                        response.StatusCode,
                                        HttpStatusCode.PartialContent,
                                        "Status Code PartialCode");
                                }

                                using (var ms = new MemoryStream())
                                {
                                    var stream = await response.Content.ReadAsStreamAsync();
                                    stream.CopyTo(ms);
                                    var data = ms.ToArray();
                                    Buffer.BlockCopy(data, 0, buffer, i * chunkSize, data.Length);
                                }
                            }
                        }

                        Assert.IsTrue(originalSet.SequenceEqual(buffer));
                    }
                }
            }

            [Test]
            public async Task GetInvalidChunk()
            {
                using (var client = new HttpClient())
                {
                    var originalSet = TestHelper.GetBigData();
                    var requestHead = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);

                    using (var res = await client.SendAsync(requestHead))
                    {
                        var remoteSize = await res.Content.ReadAsByteArrayAsync();
                        Assert.AreEqual(remoteSize.Length, originalSet.Length);

                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestHelper.BigDataFile);
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, remoteSize.Length + 10);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(
                                response.StatusCode,
                                HttpStatusCode.RequestedRangeNotSatisfiable,
                                "Status Code RequestedRangeNotSatisfiable");

                            Assert.AreEqual(response.Content.Headers.ContentRange.Length, remoteSize.Length);
                        }
                    }
                }
            }
        }

        public class CompressFile : StaticFilesModuleTest
        {
            [Test]
            public async Task GetGzip()
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.AutomaticDecompression = DecompressionMethods.GZip;

                    using (var client = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                            var html = await response.Content.ReadAsStringAsync();
                            Assert.IsNotNull(html, "Data is not empty");
                            Assert.AreEqual(Resources.Index, html);

                            // TODO: I need to fix this
                            //Assert.IsTrue(response.ContentEncoding.ToLower().Contains("gzip"), "Request is gziped");
                            //var responseStream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                        }
                    }
                }
            }
        }

        public class Etag : StaticFilesModuleTest
        {
            [Test]
            public async Task GetEtag()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                    string eTag;

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        // Can't use response.Headers.Etag, it's always null
                        Assert.NotNull(response.Headers.FirstOrDefault(x => x.Key == "ETag"), "ETag is not null");
                        eTag = response.Headers.First(x => x.Key == "ETag").Value.First();
                    }

                    var secondRequest = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                    secondRequest.Headers.TryAddWithoutValidation(HttpHeaderNames.IfNoneMatch, eTag);

                    using (var response = await client.SendAsync(secondRequest))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.NotModified, "Status Code NotModified");
                    }
                }
            }
        }
    }
}