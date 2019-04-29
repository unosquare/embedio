namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using Swan;
    using Swan.Formatters;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class WebServerTest
    {
        private const string DefaultPath = "/";
        private const int Port = 88;
        private const string Prefix = "http://localhost:9696";

        private static string[] GetMultiplePrefixes()
            => new[] {"http://localhost:9696", "http://localhost:9697", "http://localhost:9698"};

        [SetUp]
        public void Setup()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
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

        public class TaskCancellation : WebServerTest
        {
            [Test]
            public void WithCancellationRequested_ExitsSuccessfully()
            {
                var instance = new WebServer("http://localhost:9696");

                var cts = new CancellationTokenSource();
                var task = instance.RunAsync(cts.Token);
                cts.Cancel();

                task.Wait();
                instance.Dispose();

                Assert.IsTrue(task.IsCompleted);
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

            // TODO: Verify whether we need these tests, as now they aren't even compiled.
#if NETCOREAPP2_1
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

                using (var instance = new WebServer(url, RoutingStrategy.Wildcard))
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

        public class General : WebServerTest
        {
            [Test]
            public void WebMap()
            {
                var map = new Map
                {
                    Path = DefaultPath,
                    ResponseHandler = (ctx, ws) => Task.FromResult(false),
                    Verb = HttpVerbs.Any,
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
                        instance.RegisterModule(new FallbackModule((ctx, ct) =>
                            throw new InvalidOperationException("Error")));

                        var runTask = instance.RunAsync();
                        var request = new HttpClient();
                        await request.GetStringAsync(url);
                    }
                });
            }

            [Test]
            public void EmptyModules_NotFoundStatusCode()
            {
                Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    var url = Resources.GetServerAddress();

                    using (var instance = new WebServer(url))
                    {
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
                                    .FirstOrDefault(x =>
                                        x.Trim().StartsWith("charset", StringComparison.OrdinalIgnoreCase))
                                    ?
                                    .Split('=')
                                    .Skip(1)
                                    .FirstOrDefault()?
                                    .Trim();
                            encoding = Encoding.GetEncoding(encodeValue ?? throw new InvalidOperationException());
                        }
                        catch
                        {
                            Assert.Inconclusive("Invalid encoding in system");
                        }

                        return ctx.JsonResponseAsync(new EncodeCheck
                            {
                                Encoding = encoding.EncodingName,
                                IsValid = ctx.Request.ContentEncoding.EncodingName == encoding.EncodingName,
                            },
                            ct);
                    }));

                    var runTask = instance.RunAsync();

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept
                            .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                        var request = new HttpRequestMessage(HttpMethod.Post, url + TestWebModule.RedirectUrl)
                        {
                            Content = new StringContent(
                                "POST DATA", 
                                Encoding.GetEncoding(encodeName),
                                "application/json"),
                        };

                        using (var response = await client.SendAsync(request))
                        {
                            var stream = await response.Content.ReadAsStreamAsync();
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                var data = ms.ToArray().ToText();

                                Assert.IsNotNull(data, "Data is not empty");
                                var model = Json.Deserialize<EncodeCheck>(data);

                                Assert.IsNotNull(model);
                                Assert.IsTrue(model.IsValid);
                            }
                        }
                    }
                }
            }

            internal class EncodeCheck
            {
                public string Encoding { get; set; }

                public bool IsValid { get; set; }
            }
        }
    }
}