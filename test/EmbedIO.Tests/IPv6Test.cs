using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPv6Test
    {
        [TestCase("http://[::1]:8877")]
        [TestCase("http://127.0.0.1:8877")]
        [Platform("Win")]
        public async Task WebServer_WithWildcardAddress_RespondsToClient(string urlTest)
        {
            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://*:8877");
            instance.OnAny(Resources.SendTestStringAsync);

            _= instance.RunAsync();

            using var client = new HttpClient();
            Assert.IsNotEmpty(await client.GetStringAsync(urlTest));
        }

        [Test]
        [Platform("Win")]
        public async Task WithIpv6Loopback_ReturnsValid()
        {
            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://[::1]:8877");
            instance.OnAny(Resources.SendTestStringAsync);

            _ = instance.RunAsync();

            using var client = new HttpClient();
            Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
        }
    }
}
