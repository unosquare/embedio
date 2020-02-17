using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Actions;
using EmbedIO.Tests.TestObjects;
using EmbedIO.WebApi;
using NUnit.Framework;
using Swan;
using Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class WebServerTest
    {
        private const int Port = 88;
        private const string Prefix = "http://localhost:9696";

        private static string[] GetMultiplePrefixes()
            => new[] { "http://localhost:9696", "http://localhost:9697", "http://localhost:9698" };

        public class Constructors : WebServerTest
        {
            [Test]
            public void DefaultConstructor()
            {
                using var instance = new WebServer();
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
            }

            [Test]
            public void ConstructorWithPort()
            {
                using var instance = new WebServer(Port);
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
            }

            [Test]
            public void ConstructorWithSinglePrefix()
            {
                using var instance = new WebServer(Prefix);
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
            }

            [Test]
            public void ConstructorWithMultiplePrefixes()
            {
                using var instance = new WebServer(GetMultiplePrefixes());
                Assert.IsNotNull(instance.Listener, "It has a HttpListener");
                Assert.AreEqual(3, instance.Listener.Prefixes.Count);
            }
        }

        public class TaskCancellation : WebServerTest
        {
            [Test]
            public void WithCancellationRequested_ExitsSuccessfully()
            {
                using var instance = new WebServer("http://localhost:9696");
                using var cts = new CancellationTokenSource();
                var task = instance.RunAsync(cts.Token);
                cts.Cancel();
                task.Await();
                Assert.IsTrue(task.IsCompleted);
            }
        }

        public class Modules : WebServerTest
        {
            [Test]
            public void RegisterModule()
            {
                using var instance = new WebServer();
                instance.Modules.Add(nameof(WebApiModule), new WebApiModule("/"));

                Assert.AreEqual(instance.Modules.Count, 1, "It has one module");
            }
        }

        public class General : WebServerTest
        {
            [Test]
            public void ExceptionText()
            {
                Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    var url = Resources.GetServerAddress();

                    using var instance = new WebServer(url);
                    instance.Modules.Add(nameof(ActionModule), new ActionModule(_ => throw new InvalidOperationException("Error")));

                    _ = instance.RunAsync();
                    var request = new HttpClient();
                    await request.GetStringAsync(url);
                });
            }

            [Test]
            public void EmptyModules_NotFoundStatusCode()
            {
                Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    var url = Resources.GetServerAddress();

                    using var instance = new WebServer(url);
                    _ = instance.RunAsync();
                    var request = new HttpClient();
                    await request.GetStringAsync(url);
                });
            }

            [TestCase("iso-8859-1")]
            [TestCase("utf-8")]
            [TestCase("utf-16")]
            public async Task EncodingTest(string encodeName)
            {
                var url = Resources.GetServerAddress();

                using var instance = new WebServer(url);
                instance.OnPost(ctx =>
                {
                    var encoding = Encoding.GetEncoding("UTF-8");

                    try
                    {
                        var encodeValue =
                            ctx.Request.ContentType!.Split(';')
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

                    return ctx.SendDataAsync(new EncodeCheck
                    {
                        Encoding = encoding.EncodingName,
                        IsValid = ctx.Request.ContentEncoding.EncodingName == encoding.EncodingName,
                    });
                });

                _ = instance.RunAsync();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Accept
                    .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MimeType.Json));

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(
                        "POST DATA",
                        Encoding.GetEncoding(encodeName),
                        MimeType.Json),
                };

                using var response = await client.SendAsync(request);
                var data = await response.Content.ReadAsStringAsync();
                Assert.IsNotNull(data, "Data is not empty");
                var model = Json.Deserialize<EncodeCheck>(data);

                Assert.IsNotNull(model);
                Assert.IsTrue(model.IsValid);
            }

            internal class EncodeCheck
            {
                public string Encoding { get; set; } = string.Empty;

                public bool IsValid { get; set; }
            }
        }
    }
}