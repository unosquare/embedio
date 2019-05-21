using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class LocalSessionManagerTest : FixtureBase
    {
        public LocalSessionManagerTest()
            : base(ws =>
                {
                    ws.SessionManager = new LocalSessionManager
                    {
                        SessionDuration = TimeSpan.FromSeconds(1),
                    };
                    ws.Modules.Add(nameof(StaticFilesModule), new StaticFilesModule("/",TestHelper.SetupStaticFolder()));
                    ws.Modules.Add("api", new WebApiModule("/"));
                    ((WebApiModule)ws.Modules["api"]).RegisterController<TestLocalSessionController>();
                })
        {
        }

        protected async Task ValidateCookie(HttpRequestMessage request, HttpClient client, HttpClientHandler handler)
        {
            using (var response = await client.SendAsync(request))
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
            }

            Assert.IsNotNull(handler.CookieContainer, "Cookies are not null");
            Assert.Greater(handler.CookieContainer.GetCookies(new Uri(WebServerUrl)).Count,
                0,
                "Cookies are not empty");
        }

        protected async Task GetFile(string content)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = new CookieContainer();

                using (var client = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                    await ValidateCookie(request, client, handler);
                    Assert.AreNotEqual(content, handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)));
                }
            }
        }

        public class Sessions : LocalSessionManagerTest
        {
            [Test]
            public async Task DeleteSession()
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();
                    using (var client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get,
                            WebServerUrl + TestLocalSessionController.PutData);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            var body = await response.Content.ReadAsStringAsync();

                            Assert.AreEqual(TestLocalSessionController.MyData, body);
                        }

                        request = new HttpRequestMessage(HttpMethod.Get,
                            WebServerUrl + TestLocalSessionController.DeleteSession);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                            var body = await response.Content.ReadAsStringAsync();

                            Assert.AreEqual(body, "Deleted");
                        }

                        request = new HttpRequestMessage(HttpMethod.Get,
                            WebServerUrl + TestLocalSessionController.GetData);

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
                        await ValidateCookie(request, client, handler);
                        var content = handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl));
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        Task.WaitAll(
                            new[]
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

        public class Cookies : LocalSessionManagerTest
        {
            [Test]
            public async Task RetrieveCookie()
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.CookieContainer = new CookieContainer();

                    using (var client = new HttpClient(handler))
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get,
                            WebServerUrl + TestLocalSessionController.GetCookie);
                        var uri = new Uri(WebServerUrl + TestLocalSessionController.GetCookie);

                        using (var response = await client.SendAsync(request))
                        {
                            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status OK");
                            var responseCookies = handler.CookieContainer.GetCookies(uri).Cast<Cookie>();
                            Assert.IsNotNull(responseCookies, "Cookies are not null");

                            Assert.Greater(responseCookies.Count(), 0, "Cookies are not empty");
                            var cookieName =
                                responseCookies.FirstOrDefault(c => c.Name == TestLocalSessionController.CookieName);
                            Assert.AreEqual(TestLocalSessionController.CookieName, cookieName?.Name);
                        }
                    }
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

                        await ValidateCookie(request, client, handler);
                        Assert.IsNotEmpty(handler.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)),
                            "Cookie content is not null");
                    }
                }
            }
        }
    }
}