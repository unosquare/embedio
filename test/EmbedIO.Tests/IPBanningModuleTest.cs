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
                    .WithRules("(404)+")
                    .WithRules("(401)+"), 30, 50, 2)
                .WithWebApi("/api", m => m.RegisterController<TestController>());
        }

        private HttpRequestMessage GetNotFoundRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/notFound");

        private HttpRequestMessage GetEmptyRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/empty");

        private HttpRequestMessage GetUnauthorizedRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/unauthorized");

        private IPAddress LocalHost { get; } = IPAddress.Parse("127.0.0.1");

        [Test]
        public async Task RequestFailRegex_ReturnsForbidden()
        {
            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetUnauthorizedRequest());
            var response = await Client.SendAsync(GetNotFoundRequest());

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpMinutes_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(LocalHost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(LocalHost, 10);

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpTimeSpan_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(LocalHost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(LocalHost, TimeSpan.FromMinutes(10));

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task BanIpDateTime_ReturnsForbidden()
        {
            IPBanningModule.TryUnbanIP(LocalHost);

            var response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "Status Code NotFound");

            IPBanningModule.TryBanIP(LocalHost, DateTime.Now.AddMinutes(10));

            response = await Client.SendAsync(GetNotFoundRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task RequestFailRegex_UnbanIp_ReturnsNotFound()
        {
            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetNotFoundRequest());
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
            IPBanningModule.TryUnbanIP(LocalHost);

            foreach (var x in Enumerable.Range(0, 100))
            {
                await Client.SendAsync(GetEmptyRequest());
            }
            
            var response = await Client.SendAsync(GetEmptyRequest());
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }
    }
}