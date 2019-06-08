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
            : base(ws => ws.WithStaticFolderAt("/", StaticFolder.RootPathOf(nameof(StaticFilesModuleTest))))
        {
            ServedFolder = new StaticFolder.WithDataFiles(nameof(StaticFilesModuleTest));
        }

        protected StaticFolder.WithDataFiles ServedFolder { get; }

        protected override void Dispose(bool disposing)
        {
            ServedFolder.Dispose();
        }

        private async Task ValidatePayload(HttpResponseMessage response, int offset, int maxLength)
        {
            Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Status Code PartialCode");

            using (var ms = new MemoryStream())
            {
                var responseStream = await response.Content.ReadAsStreamAsync();
                responseStream.CopyTo(ms);
                var data = ms.ToArray();

                Assert.IsNotNull(data, "Data is not empty");
                Assert.IsTrue(ServedFolder.BigData.Skip(offset).Take(maxLength).SequenceEqual(data));
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
                    Assert.Ignore("File-system is not case sensitive.");
                }

                var htmlUpperCase = await GetString(StaticFolder.WithDataFiles.UppercaseFile);
                Assert.AreEqual(nameof(StaticFolder.WithDataFiles.UppercaseFile), htmlUpperCase, "Same content upper case");

                var htmlLowerCase = await GetString(StaticFolder.WithDataFiles.LowercaseFile);
                Assert.AreEqual(nameof(StaticFolder.WithDataFiles.LowercaseFile), htmlLowerCase, "Same content lower case");
            }
        }

        public class GetPartials : StaticFilesModuleTest
        {
            [TestCase("Got initial part of file", 0, 1024)]
            [TestCase("Got middle part of file", StaticFolder.WithDataFiles.BigDataSize / 2, 1024)]
            [TestCase("Got final part of file", StaticFolder.WithDataFiles.BigDataSize - 1024, 1024)]
            public async Task GetPartialContent(string message, int offset, int length)
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, offset + length - 1);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent, "Responds with 216 Partial Content");

                        using (var ms = new MemoryStream())
                        {
                            var responseStream = await response.Content.ReadAsStreamAsync();
                            responseStream.CopyTo(ms);
                            var data = ms.ToArray();
                            Assert.IsTrue(ServedFolder.BigData.Skip(offset).Take(length).SequenceEqual(data), message);
                        }
                    }
                }
            }

            [Test]
            public async Task NotPartial()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);

                    using (var response = await client.SendAsync(request))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        using (var ms = new MemoryStream())
                        {
                            var responseStream = await response.Content.ReadAsStreamAsync();
                            responseStream.CopyTo(ms);
                            var data = ms.ToArray();

                            Assert.IsNotNull(data, "Data is not empty");
                            Assert.IsTrue(ServedFolder.BigData.SequenceEqual(data));
                        }
                    }
                }
            }

            [Test]
            public async Task ReconstructFileFromPartials()
            {
                using (var client = new HttpClient())
                {
                    var requestHead = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);

                    int remoteSize;
                    using (var res = await client.SendAsync(requestHead))
                    {
                        remoteSize = (await res.Content.ReadAsByteArrayAsync()).Length;
                    }

                    Assert.AreEqual(remoteSize, StaticFolder.WithDataFiles.BigDataSize);

                    var buffer = new byte[remoteSize];
                    const int chunkSize = 100000;
                    for (var offset = 0; offset < remoteSize; offset += chunkSize)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);
                        var top = Math.Min(offset + chunkSize, remoteSize) - 1;

                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, top);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.PartialContent);

                            using (var ms = new MemoryStream())
                            {
                                var stream = await response.Content.ReadAsStreamAsync();
                                stream.CopyTo(ms);
                                Buffer.BlockCopy(ms.GetBuffer(), 0, buffer, offset, (int)ms.Length);
                            }
                        }
                    }

                    Assert.IsTrue(ServedFolder.BigData.SequenceEqual(buffer));
                }
            }

            [Test]
            public async Task InvalidRange_RespondsWith416()
            {
                using (var client = new HttpClient())
                {
                    var requestHead = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);

                    using (var res = await client.SendAsync(requestHead))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + StaticFolder.WithDataFiles.BigDataFile);
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, StaticFolder.WithDataFiles.BigDataSize + 10);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.RequestedRangeNotSatisfiable);
                            Assert.AreEqual(response.Content.Headers.ContentRange.Length, StaticFolder.WithDataFiles.BigDataSize);
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