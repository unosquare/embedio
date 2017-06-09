namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Net;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;
    using Modules;
    using TestObjects;
    using System;
    using System.IO;
    using System.Text;
    using Unosquare.Swan.Formatters;

    [TestFixture]
    public class WebServerTest
    {
        private const string DefaultPath = "/";

        [SetUp]
        public void Setup()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;
        }

        [Test]
        public void WebServerDefaultConstructor()
        {
            var instance = new WebServer();
            Assert.IsNotNull(instance.Listener, "It has a HttpListener");
            Assert.IsNotNull(Constants.DefaultMimeTypes, "It has MimeTypes");
        }

        [Test]
        public void WebserverCanBeDisposed()
        {
            Assert.Ignore("This test is not longer valid, rewrite it");
            //var cts = new CancellationTokenSource();
            //var instance = new WebServer(Resources.GetServerAddress());
            //var task = instance.RunAsync(cts.Token);

            //cts.Cancel();

            //try
            //{
            //    //Thread.Sleep(2000);
            //    task.Wait();
            //}
            //catch (AggregateException e)
            //{
            //    var baseEx = e.GetBaseException();
            //    if (baseEx is OperationCanceledException)
            //    {
            //        instance.Dispose();
            //        return;
            //    }

            //    Assert.Fail($"Must have thrown OperationCanceledException and threw '{baseEx.GetType()}' instead.");
            //}
            //catch (Exception ex)
            //{
            //    Assert.Fail($"Must have thrown AggregateException and threw '{ex.GetType()}' instead.");
            //}
        }

        [Test]
        public void WebServerCanBeRestarted()
        {
            Assert.Ignore("This test is not longer valid, rewrite it");
            //var cts = new CancellationTokenSource();
            //var instance = new WebServer(Resources.GetServerAddress());
            //var task = instance.RunAsync(cts.Token);

            ////need to make a request here for it to fail before the cancellation changes, null works, yay
            //instance.ProcessRequest(null);

            //cts.Cancel();

            //try
            //{
            //    //Thread.Sleep(2000);
            //    task.Wait();
            //}
            //catch (AggregateException e)
            //{
            //    var baseEx = e.GetBaseException();
            //    if (baseEx is OperationCanceledException)
            //    {
            //        instance.Dispose();
            //        return;
            //    }
            //}

            //cts = new CancellationTokenSource();
            //instance = new WebServer(Resources.GetServerAddress());
            //task = instance.RunAsync(cts.Token);

            //cts.Cancel();

            //try
            //{
            //    //Thread.Sleep(2000);
            //    task.Wait();
            //}
            //catch (AggregateException e)
            //{
            //    var baseEx = e.GetBaseException();

            //    if (baseEx is OperationCanceledException)
            //    {
            //        instance.Dispose();
            //    }
            //}
        }

        [Test]
        public void RegisterAndUnregisterModule()
        {
            var instance = new WebServer();
            instance.RegisterModule(new LocalSessionModule());

            Assert.AreEqual(instance.Modules.Count, 1, "It has one module");

            instance.UnregisterModule(typeof(LocalSessionModule));

            Assert.AreEqual(instance.Modules.Count, 0, "It has not modules");
        }

        [Test]
        public void WebMap()
        {
            var map = new Map() { Path = DefaultPath, ResponseHandler = (ctx, ws) => Task.FromResult(false), Verb = HttpVerbs.Any };

            Assert.AreEqual(map.Path, DefaultPath, "Default Path is correct");
            Assert.AreEqual(map.Verb, HttpVerbs.Any, "Default Verb is correct");
        }

        [Test]
        public void WebModuleAddHandler()
        {
            var webModule = new TestWebModule();
            // add one more handler
            webModule.AddHandler(DefaultPath, HttpVerbs.Any, (ctx, ws) => Task.FromResult(false));

            Assert.AreEqual(webModule.Handlers.Count, 4, "WebModule has four handlers");
            Assert.AreEqual(webModule.Handlers.Last().Path, DefaultPath, "Default Path is correct");
            Assert.AreEqual(webModule.Handlers.Last().Verb, HttpVerbs.Any, "Default Verb is correct");
        }

        internal class EncodeCheck
        {
            public string Encoding { get; set; }

            public bool IsValid { get; set; }
        }

        public void ExceptionText()
        {
            Assert.ThrowsAsync<WebException>(async () =>
            {
                var url = Resources.GetServerAddress();

                using (var instance = new WebServer(url))
                {
                    instance.RegisterModule(new FallbackModule((ctx, ct) =>
                    {
                        throw new Exception("Error");
                    }));

                    var runTask = instance.RunAsync();
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    await request.GetResponseAsync();
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
                                .FirstOrDefault(x => x.Trim().StartsWith("charset", StringComparison.OrdinalIgnoreCase))?
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

                    ctx.JsonResponse(new EncodeCheck { Encoding = encoding.EncodingName, IsValid = ctx.Request.ContentEncoding.EncodingName == encoding.EncodingName });

                    return true;
                }));

                var runTask = instance.RunAsync();

                var request = (HttpWebRequest)WebRequest.Create(url + TestWebModule.RedirectUrl);
                request.Method = "POST";
                request.ContentType = $"application/json; charset={encodeName}";

                var byteArray = Encoding.GetEncoding(encodeName).GetBytes("POST DATA");
#if NET452 || NET46
                request.ContentLength = byteArray.Length;
#endif
                var requestStream = await request.GetRequestStreamAsync();
                requestStream.Write(byteArray, 0, byteArray.Length);

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (var ms = new MemoryStream())
                    {
                        response.GetResponseStream()?.CopyTo(ms);
                        var data = Encoding.UTF8.GetString(ms.ToArray());

                        Assert.IsNotNull(data, "Data is not empty");
                        var model = Json.Deserialize<EncodeCheck>(data);

                        Assert.IsNotNull(model);
                        Assert.IsTrue(model.IsValid);
                    }
                }
            }
        }

#if NET452 || NET46
        [Test]
        public async Task TestWebModuleRedirect()
        {
            var url = Resources.GetServerAddress();

            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new TestWebModule());
                var runTask = instance.RunAsync();

                var request = (HttpWebRequest)WebRequest.Create(url + TestWebModule.RedirectUrl);
                request.AllowAutoRedirect = false;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Redirect, "Status Code Redirect");
                }
            }
        }

        [Test]
        public async Task TestWebModuleAbsoluteRedirect()
        {
            var url = Resources.GetServerAddress();

            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new TestWebModule());
                var runTask = instance.RunAsync();

                var request = (HttpWebRequest)WebRequest.Create(url + TestWebModule.RedirectAbsoluteUrl);
                request.AllowAutoRedirect = false;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Redirect, "Status Code Redirect");
                }
            }
        }
#endif
    }
}