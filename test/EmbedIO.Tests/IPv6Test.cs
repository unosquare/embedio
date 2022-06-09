using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using Swan;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPv6Test
    {
        [TestCase("http://[::1]:8877")]
        [TestCase("http://127.0.0.1:8877")]
        public async Task WebServer_WithWildcardAddress_RespondsToClient(string urlTest)
        {
            if (SwanRuntime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://*:8877");
            instance.OnAny(Resources.SendTestStringAsync);

            _= instance.RunAsync();

            using var client = new HttpClient();
            Assert.IsNotEmpty(await client.GetStringAsync(urlTest));
        }

        [Test]
        public async Task WithIpv6Loopback_ReturnsValid()
        {
            if (SwanRuntime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://[::1]:8877");
            instance.OnAny(Resources.SendTestStringAsync);

            _ = instance.RunAsync();

            using var client = new HttpClient();
            Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
        }
    }
}
