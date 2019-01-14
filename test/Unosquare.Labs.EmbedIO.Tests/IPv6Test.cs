namespace Unosquare.Labs.EmbedIO.Tests
{
    using Net;
    using NUnit.Framework;
    using Swan;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    [TestFixture]
    public class IPv6Test
    {
        [SetUp]
        public void Setup()
        {
            EndPointManager.UseIpv6 = true;

            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
        }

        [Test]
        public async Task WithUseIpv6_ReturnsValid()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(new[] { "http://*:8877" }, Constants.RoutingStrategy.Regex, HttpListenerMode.EmbedIO);
            instance.OnAny((ctx, ct) => ctx.JsonResponseAsync(DateTime.Now, ct));

            instance.RunAsync();

            using (var client = new HttpClient())
            {
                Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
                Assert.IsNotEmpty(await client.GetStringAsync("http://127.0.0.1:8877"));
                Assert.IsNotEmpty(await client.GetStringAsync("http://localhost:8877"));
            }
        }

        [Test]
        public async Task WithIpv6Loopback_ReturnsValid()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(new[] { "http://[::1]:8877" }, Constants.RoutingStrategy.Regex, HttpListenerMode.EmbedIO);
            instance.OnAny((ctx, ct) => ctx.JsonResponseAsync(DateTime.Now, ct));

            instance.RunAsync();

            using (var client = new HttpClient())
            {
                Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
                Assert.IsNotEmpty(await client.GetStringAsync("http://localhost:8877"));
            }
        }

        [TearDown]
        public void TearDown()
        {
            EndPointManager.UseIpv6 = false;
        }
    }
}
