namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using NUnit.Framework;
    using Swan.Formatters;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

    [TestFixture]
    public class CorsModuleTest
    {
        protected WebServer WebServer;
        protected string WebServerUrl;
        protected object TestObj = new { Message = "OK" };

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            WebServer = new WebServer(WebServerUrl)
                .EnableCors(
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                    "content-type",
                    "post,get");

            WebServer.RegisterModule(new WebApiModule());
            WebServer.Module<WebApiModule>().RegisterController<TestController>();
            WebServer.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse(TestObj)));
            WebServer.RunAsync();
        }

        [Test]
        public async Task GetFallback()
        {
            var webClient = new HttpClient();

            var jsonBody = await webClient.GetStringAsync(WebServerUrl + "invalidpath");
            
            Assert.AreEqual(Json.Serialize(TestObj), jsonBody, "Same content");
        }

        [Test]
        public async Task PreFlight()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestController.GetPath);
            request.Headers[Headers.Origin] = "http://unosquare.github.io";
            request.Headers[Headers.AccessControlRequestMethod] = "post";
            request.Headers[Headers.AccessControlRequestHeaders] = "content-type";
            request.Method = "OPTIONS";

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
            }
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            WebServer.Dispose();
        }
    }
}