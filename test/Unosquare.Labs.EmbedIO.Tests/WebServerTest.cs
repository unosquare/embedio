namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Net;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;
    using Modules;
    using TestObjects;

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
            var map = new Map() {Path = DefaultPath, ResponseHandler = (ctx, ws) => Task.FromResult(false), Verb = HttpVerbs.Any};

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

#if !NETCOREAPP1_1
        [Test]
        public async Task TestWebModuleRedirect()
        {
            var url = Resources.GetServerAddress();

            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new TestWebModule());
                instance.RunAsync();

                var request = (HttpWebRequest) WebRequest.Create(url + TestWebModule.RedirectUrl);
                request.AllowAutoRedirect = false;

                using (var response = (HttpWebResponse) await request.GetResponseAsync())
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
                instance.RunAsync();

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