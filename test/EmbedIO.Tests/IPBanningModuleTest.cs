using EmbedIO.Security;
using EmbedIO.Tests.TestObjects;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Linq;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPBanningModuleTest : EndToEndFixtureBase
    {
        protected override void OnSetUp()
        {
            Server
                .WithIPBanning(o => o
                    .WithRegexRules(2, 60, "(404)+", "(401)+")
                    .WithMaxRequestsPerSecond())
                .WithWebApi("/api", m => m.RegisterController<TestController>());
        }

        private HttpRequestMessage GetNotFoundRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/notFound");

        private HttpRequestMessage GetEmptyRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/empty");

        private HttpRequestMessage GetUnauthorizedRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/unauthorized");

        private IPAddress Localhost { get; } = IPAddress.Parse("127.0.0.1");

        [Test]
        public async Task RequestFailRegex_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetUnauthorizedRequest());
            _ = await Client.SendAsync(GetNotFoundRequest());

            // Giving some time for logging
            await Task.Delay(200);
            var response = await Client.SendAsync(GetNotFoundRequest());

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpMinutes_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(Localhost, 10);

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpTimeSpan_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(Localhost, TimeSpan.FromMinutes(10));

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpDateTime_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(Localhost, DateTime.Now.AddMinutes(10));

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task RequestFailRegex_UnbanIp_ReturnsNotFound()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetNotFoundRequest());
            
            // Giving some time for logging
            await Task.Delay(200);
            var response = await Client.SendAsync(GetNotFoundRequest());

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");

            var bannedIps = IPBanningModule.GetBannedIPs();

            foreach (var address in bannedIps)
                IPBanningModule.TryUnbanIP(address.IPAddress);

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");
        }

        [Test]
        public async Task MaxRps_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(Localhost);

            foreach (var _ in Enumerable.Range(0, 100))
            {
                await Client.SendAsync(GetEmptyRequest());
            }
            
            var response = await Client.SendAsync(GetEmptyRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }
    }
}