using EmbedIO.Sessions;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class LocalSessionManagerTest : EndToEndFixtureBase
    {
        public LocalSessionManagerTest()
            : base(false)
        {
        }

        protected override void OnSetUp()
        {
            Server
                .WithSessionManager(new LocalSessionManager {
                    SessionDuration = TimeSpan.FromSeconds(1),
                })
                .WithWebApi("/api", m => m.RegisterController<TestLocalSessionController>())
                .OnGet(ctx =>
                {
                    ctx.Session["data"] = true;
                    ctx.Response.SetEmptyResponse((int)HttpStatusCode.OK);
                    return Task.FromResult(true);
                });
        }

        protected void ClearServerCookies()
        {
            foreach (var cookie in Client.CookieContainer.GetCookies(new Uri(WebServerUrl)).Cast<Cookie>())
            {
                cookie.Expired = true;
            }
        }

        protected async Task ValidateCookie(HttpRequestMessage request)
        {
            using (var response = await Client.SendAsync(request))
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");
            }

            Assert.IsNotNull(Client.CookieContainer, "Cookies are not null");
            Assert.Greater(
                Client.CookieContainer.GetCookies(new Uri(WebServerUrl)).Count,
                0,
                "Cookies are not empty");
        }

        public class Sessions : LocalSessionManagerTest
        {
            [Test]
            public async Task DeleteSession()
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    WebServerUrl + TestLocalSessionController.PutDataPath);

                using (var response = await Client.SendAsync(request))
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");

                    var body = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual(TestLocalSessionController.MyData, body);
                }

                request = new HttpRequestMessage(HttpMethod.Get,
                    WebServerUrl + TestLocalSessionController.DeleteSessionPath);

                using (var response = await Client.SendAsync(request))
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");

                    var body = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual("Deleted", body);
                }

                request = new HttpRequestMessage(HttpMethod.Get,
                    WebServerUrl + TestLocalSessionController.GetDataPath);

                using (var response = await Client.SendAsync(request))
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");

                    var body = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual(string.Empty, body);
                }
            }

            [Test]
            public async Task GetDifferentSession()
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                await ValidateCookie(request);
                var firstCookie = Client.CookieContainer.GetCookieHeader(new Uri(WebServerUrl));

                request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                await ValidateCookie(request);
                Assert.AreEqual(firstCookie, Client.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)));

                ClearServerCookies();

                request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                await ValidateCookie(request);
                Assert.AreNotEqual(firstCookie, Client.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)));
            }
        }

        public class Cookies : LocalSessionManagerTest
        {
            [Test]
            public async Task RetrieveCookie()
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    WebServerUrl + TestLocalSessionController.GetCookiePath);
                var uri = new Uri(WebServerUrl + TestLocalSessionController.GetCookiePath);

                using var response = await Client.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status OK");
                var responseCookies = Client.CookieContainer.GetCookies(uri).Cast<Cookie>();
                Assert.IsNotNull(responseCookies, "Cookies are not null");

                Assert.Greater(responseCookies.Count(), 0, "Cookies are not empty");
                var cookieName = responseCookies.FirstOrDefault(c => c.Name == TestLocalSessionController.CookieName);
                Assert.AreEqual(TestLocalSessionController.CookieName, cookieName?.Name);
            }

            [Test]
            public async Task GetCookie()
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                await ValidateCookie(request);
                Assert.IsNotEmpty(
                    Client.CookieContainer.GetCookieHeader(new Uri(WebServerUrl)),
                    "Cookie content is not null");
            }
        }
    }
}