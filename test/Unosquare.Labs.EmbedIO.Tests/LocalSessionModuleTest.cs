namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;
    using System.IO;
    using System.Net.Http;
    using Unosquare.Swan.Formatters;

    [TestFixture]
    public class LocalSessionModuleTest : FixtureBase
    {
        private const string CookieName = "__session";
        protected WebServer WebServer;
        protected TimeSpan WaitTimeSpan = TimeSpan.FromSeconds(1);

        public LocalSessionModuleTest()
            : base((ws) =>
            {
                ws.RegisterModule((new LocalSessionModule() { Expiration = TimeSpan.FromSeconds(1) }));
                ws.RegisterModule(new StaticFilesModule(TestHelper.SetupStaticFolder()));
                ws.RegisterModule(new WebApiModule());
                ws.Module<WebApiModule>().RegisterController<TestLocalSessionController>();
            }, Constants.RoutingStrategy.Wildcard)
        {
        }

        protected async Task GetFile(string content)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = new CookieContainer();
                using (var client = new HttpClient(handler))
                {
                    var secondRequest = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                    using (var response = await client.SendAsync(secondRequest))
                    {
                        Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                        Assert.IsNotNull(handler.CookieContainer, "Cookies are not null");
                        Assert.Greater(handler.CookieContainer.GetCookies(new Uri(WebServerUrl)).Count, 0, "Cookies are not empty");

                        Assert.AreNotEqual(content, handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)).ToString());
                    }
                }
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
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();
                    using (var client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestLocalSessionController.PutData);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            var body = await response.Content.ReadAsStringAsync();

                            Assert.AreEqual(body, TestLocalSessionController.MyData);
                        }

                        request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestLocalSessionController.DeleteSession);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            var body = await response.Content.ReadAsStringAsync();

                            Assert.AreEqual(body, "Deleted");
                        }

                        request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + TestLocalSessionController.GetData);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            var body = await response.Content.ReadAsStringAsync();

                            Assert.AreEqual(string.Empty, body);
                        }
                    }
                }
            }

            [Test]
            public async Task GetDifferentSession()
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();
                    using (var client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                        string content;

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            Assert.IsNotNull(handler.CookieContainer, "Cookies are not null");
                            Assert.Greater(handler.CookieContainer.GetCookies(new Uri(WebServerUrl)).Count, 0, "Cookies are not empty");

                            content = handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)).ToString();
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
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();
                    using (var client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            Assert.IsNotNull(handler.CookieContainer, "Cookies are not null");
                            Assert.Greater(handler.CookieContainer.GetCookies(new Uri(WebServerUrl)).Count, 0, "Cookies are not empty");

                            Assert.IsNotEmpty(handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)).ToString(), "Cookie content is not null");
                        }
                    }
                }
            }
        }

    }
}