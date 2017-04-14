namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;
    using System.IO;

    [TestFixture]
    public class LocalSessionModuleTest
    {
        private const string CookieName = "__session";
        protected string RootPath;
        protected WebServer WebServer;

        protected string WebServerUrl;
        protected TimeSpan WaitTimeSpan = TimeSpan.FromSeconds(1);

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            RootPath = TestHelper.SetupStaticFolder();

            WebServer = new WebServer(WebServerUrl);
            WebServer.RegisterModule(new LocalSessionModule() { Expiration = WaitTimeSpan });
            WebServer.RegisterModule(new StaticFilesModule(RootPath));
            WebServer.RegisterModule(new WebApiModule());
            WebServer.Module<WebApiModule>().RegisterController<TestLocalSessionController>();
            WebServer.RunAsync();
        }

        [Test]
        public void HasSessionModule()
        {
            Assert.IsNotNull(WebServer.SessionModule, "Session module is not null");
            Assert.AreEqual(WebServer.SessionModule.Handlers.Count, 1, "Session module has one handler");
        }

        [Test]
        public async Task GetCookie()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            request.CookieContainer = new CookieContainer();

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                Assert.IsNotNull(response.Cookies, "Cookies are not null");
                Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");

                var content = response.Cookies[CookieName]?.Value;

                Assert.IsNotEmpty(content, "Cookie content is not null");
            }
        }

        [Test]
        public async Task GetDifferentSession()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            request.CookieContainer = new CookieContainer();
            string content;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                Assert.IsNotNull(response.Cookies, "Cookies are not null");
                Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");

                content = response.Cookies[CookieName]?.Value;
            }

            await Task.Delay(WaitTimeSpan);

            Task.WaitAll(new[] {
                Task.Factory.StartNew(() => GetFile(content)),
                Task.Factory.StartNew(() => GetFile(content)),
                Task.Factory.StartNew(() => GetFile(content)),
                Task.Factory.StartNew(() => GetFile(content)),
                Task.Factory.StartNew(() => GetFile(content)),
            });
        }

        protected async Task GetFile(string content)
        {
            var secondRequest = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            secondRequest.CookieContainer = new CookieContainer();

            using (var response = (HttpWebResponse)await secondRequest.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                Assert.IsNotNull(response.Cookies, "Cookies are not null");
                Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");

                Assert.AreNotEqual(content, response.Cookies[CookieName]?.Value);
            }
        }

        [Test]
        public async Task DeleteSession()
        {
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl);
            CookieContainer container = new CookieContainer();
            request.CookieContainer = container;

            request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.PutData);

            request.CookieContainer = container;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, TestLocalSessionController.MyData);
            }

            request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.GetData);

            request.CookieContainer = container;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, TestLocalSessionController.MyData);
            }

            request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.DeleteSession);
            request.CookieContainer = container;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, "Deleted");
            }

            request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.GetData);

            request.CookieContainer = container;

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual("", body);
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