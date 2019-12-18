using EmbedIO.Security;
using EmbedIO.Tests.TestObjects;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPBanningModuleTest : EndToEndFixtureBase
    {
        protected override void OnSetUp()
        {
            Server
                .WithIPBanning(o => o
                    .WithRules("(404)+"), 1, 2)
                .WithWebApi("/api", m => m.RegisterController<TestController>());
        }

        private HttpRequestMessage GetNotFoundRequest() =>
            new HttpRequestMessage(HttpMethod.Get, $"{WebServerUrl}/api/notFound");

        [Test]
        public async Task RequestInvalidRule_ReturnsForbidden()
        {
            _ = await Client.SendAsync(GetNotFoundRequest());
            _ = await Client.SendAsync(GetNotFoundRequest());
            var response = await Client.SendAsync(GetNotFoundRequest());

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode, "Status Code Forbidden");
        }

        [Test]
        public async Task RequestInvalidRule_UnbanIp_ReturnsNotFound()
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
    }
}