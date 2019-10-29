using NUnit.Framework;
using Swan;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPv6Test
    {
        [TestCase("http://[::1]:8877")]
        [TestCase("http://127.0.0.1:8877")]
        public async Task WithUseIpv6_ReturnsValid(string urlTest)
        {
            if (SwanRuntime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://*:8877");
            instance.OnAny(ctx => ctx.SendDataAsync(DateTime.Now));

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
            instance.OnAny(ctx => ctx.SendDataAsync(DateTime.Now));

            _ = instance.RunAsync();

            using var client = new HttpClient();
            Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
        }
    }
}
