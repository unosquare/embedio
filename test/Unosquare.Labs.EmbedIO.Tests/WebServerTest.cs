﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Net.Http;
    using Constants;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using System.Linq;
    using Modules;
    using TestObjects;
    using System;
    using System.IO;
    using System.Text;
    using Swan.Formatters;

    [TestFixture]
    public class WebServerTest
    {
        private const string DefaultPath = "/";
        private const int Port = 88;
        private const string Prefix = "http://localhost:9696";

        private static string[] GetMultiplePrefixes()
            => new[] {"http://localhost:9696", "http://localhost:9697", "http://localhost:9698"};

        internal class EncodeCheck
        {
            public string Encoding { get; set; }

            public bool IsValid { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;
        }

        public class Constructors : WebServerTest
        {
            [Test]
            public void DefaultConstructor()
            {
                var instance = new WebServer();
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
                Assert.IsNotNull(MimeTypes.DefaultMimeTypes, "It has MimeTypes");
            }

            [Test]
            public void ConstructorWithPort()
            {
                var instance = new WebServer(Port);
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
                Assert.IsNotNull(MimeTypes.DefaultMimeTypes, "It has MimeTypes");
            }

            [Test]
            public void ConstructorWithSinglePrefix()
            {
                var instance = new WebServer(Prefix);
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
                Assert.IsNotNull(MimeTypes.DefaultMimeTypes, "It has MimeTypes");
            }

            [Test]
            public void ConstructorWithMultiplePrefixes()
            {
                var instance = new WebServer(GetMultiplePrefixes());
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
                Assert.AreEqual(instance.Listener.Prefixes.Count, 3);
            }
        }

        public class Modules : WebServerTest
        {
            [Test]
            public void RegisterAndUnregister()
            {
                var instance = new WebServer();
                instance.RegisterModule(new LocalSessionModule());

                Assert.AreEqual(instance.Modules.Count, 1, "It has one module");

                instance.UnregisterModule(typeof(LocalSessionModule));

                Assert.AreEqual(instance.Modules.Count, 0, "It has not modules");
            }

            [Test]
            public void AddHandler()
            {
                var webModule = new TestWebModule();
                webModule.AddHandler(DefaultPath, HttpVerbs.Any, (ctx, ws) => Task.FromResult(false));

                Assert.AreEqual(webModule.Handlers.Count, 4, "WebModule has four handlers");
                Assert.AreEqual(webModule.Handlers.Last().Path, DefaultPath, "Default Path is correct");
                Assert.AreEqual(webModule.Handlers.Last().Verb, HttpVerbs.Any, "Default Verb is correct");
            }

#if NETCOREAPP2_0
            [Test]
            public async Task Redirect()
            {
                var url = Resources.GetServerAddress();

                using (var instance = new WebServer(url))
                {
                    instance.RegisterModule(new TestWebModule());
                    var runTask = instance.RunAsync();
                    using (var handler = new HttpClientHandler())
                    {
                        handler.AllowAutoRedirect = false;
                        using (var client = new HttpClient(handler))
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, url + TestWebModule.RedirectUrl);
                            using (var response = await client.SendAsync(request))
                            {
                                Assert.AreEqual(System.Net.HttpStatusCode.Redirect, response.StatusCode);
                            }
                        }
                    }
                }
            }

            [Test]
            public async Task AbsoluteRedirect()
            {
                var url = Resources.GetServerAddress();

                using (var instance = new WebServer(url))
                {
                    instance.RegisterModule(new TestWebModule());
                    var runTask = instance.RunAsync();
                    using (var handler = new HttpClientHandler())
                    {
                        handler.AllowAutoRedirect = false;
                        using (var client = new HttpClient(handler))
                        {
                            var request =
                                new HttpRequestMessage(HttpMethod.Get, url + TestWebModule.RedirectAbsoluteUrl);

                            using (var response = await client.SendAsync(request))
                            {
                                Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
                            }
                        }
                    }
                }
            }
#endif
        }

        [Test]
        public void WebMap()
        {
            var map = new Map
            {
                Path = DefaultPath,
                ResponseHandler = (ctx, ws) => Task.FromResult(false),
                Verb = HttpVerbs.Any
            };

            Assert.AreEqual(map.Path, DefaultPath, "Default Path is correct");
            Assert.AreEqual(map.Verb, HttpVerbs.Any, "Default Verb is correct");
        }

        [Test]
        public void ExceptionText()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                var url = Resources.GetServerAddress();

                using (var instance = new WebServer(url))
                {
                    instance.RegisterModule(new FallbackModule((ctx, ct) => throw new Exception("Error")));

                    var runTask = instance.RunAsync();
                    var request = new HttpClient();
                    await request.GetStringAsync(url);
                }
            });
        }

        [TestCase("iso-8859-1")]
        [TestCase("utf-8")]
        [TestCase("utf-16")]
        public async Task EncodingTest(string encodeName)
        {
            // NOTE: This is failing with NET46

            var url = Resources.GetServerAddress();

            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new FallbackModule((ctx, ct) =>
                {
                    var encoding = Encoding.GetEncoding("UTF-8");

                    try
                    {
                        var encodeValue =
                            ctx.Request.ContentType.Split(';')
                                .FirstOrDefault(x => x.Trim().StartsWith("charset", StringComparison.OrdinalIgnoreCase))
                                ?
                                .Split('=')
                                .Skip(1)
                                .FirstOrDefault()?
                                .Trim();
                        encoding = Encoding.GetEncoding(encodeValue);
                    }
                    catch
                    {
                        Assert.Inconclusive("Invalid encoding in system");
                    }

                    ctx.JsonResponse(new EncodeCheck
                    {
                        Encoding = encoding.EncodingName,
                        IsValid = ctx.Request.ContentEncoding.EncodingName == encoding.EncodingName
                    });

                    return true;
                }));

                var runTask = instance.RunAsync();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept
                        .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var request = new HttpRequestMessage(HttpMethod.Post, url + TestWebModule.RedirectUrl)
                    {
                        Content = new StringContent("POST DATA", Encoding.GetEncoding(encodeName), "application/json")
                    };

                    using (var response = await client.SendAsync(request))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            var data = Encoding.UTF8.GetString(ms.ToArray());

                            Assert.IsNotNull(data, "Data is not empty");
                            var model = Json.Deserialize<EncodeCheck>(data);

                            Assert.IsNotNull(model);
                            Assert.IsTrue(model.IsValid);
                        }
                    }
                }
            }
        }
    }
}