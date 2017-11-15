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
    public class LocalSessionModuleTest : FixtureBase
    {
        private const string CookieName = "__session";
        protected WebServer WebServer;
        protected TimeSpan WaitTimeSpan = TimeSpan.FromSeconds(1);

        public LocalSessionModuleTest()
            :base((ws) => {
                ws.RegisterModule((new LocalSessionModule() { Expiration = TimeSpan.FromSeconds(1) }));
                ws.RegisterModule(new StaticFilesModule(TestHelper.SetupStaticFolder()));
                ws.RegisterModule(new WebApiModule());
                ws.Module<WebApiModule>().RegisterController<TestLocalSessionController>();
            }, Constants.RoutingStrategy.Wildcard)
        {
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
        public class Sessions : LocalSessionModuleTest
        {
            [Test]
            public void HasSessionModule()
            {
                Assert.IsNotNull(_webServer.SessionModule, "Session module is not null");
                Assert.AreEqual(_webServer.SessionModule.Handlers.Count, 1, "Session module has one handler");
            }

            [Test]
            public async Task DeleteSession()
            {
                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.PutData);
                request.CookieContainer = new CookieContainer();

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    Assert.AreEqual(body, TestLocalSessionController.MyData);
                }

                request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.DeleteSession);
                request.CookieContainer = new CookieContainer();

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    Assert.AreEqual(body, "Deleted");
                }

                request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.GetData);
                request.CookieContainer = new CookieContainer();

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    Assert.AreEqual(string.Empty, body);
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

                Task.WaitAll(new[]
                {
                    Task.Factory.StartNew(() => GetFile(content)),
                    Task.Factory.StartNew(() => GetFile(content)),
                    Task.Factory.StartNew(() => GetFile(content)),
                    Task.Factory.StartNew(() => GetFile(content)),
                    Task.Factory.StartNew(() => GetFile(content)),
                });
            }
        }

        public class Cookies : LocalSessionModuleTest
        {
            [Test]
            public async Task RetrieveCookie()
            {
                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestLocalSessionController.GetCookie);
                request.CookieContainer = new CookieContainer();

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    Assert.IsNotNull(response.Cookies, "Cookies are not null");
                    Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");

                    Assert.AreEqual(TestLocalSessionController.CookieName, response.Cookies[TestLocalSessionController.CookieName]?.Value);
                }
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
        }

    }
}