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
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
        }

        [Test]
        public async Task WithUseIpv6_ReturnsValid()
        {
            EndPointManager.UseIpv6 = true;

            var instance = new WebServer(new[] { "http://*:8877" }, Constants.RoutingStrategy.Regex, HttpListenerMode.EmbedIO);
            instance.OnAny((ctx, ct) => ctx.JsonResponseAsync(DateTime.Now, ct));

            instance.RunAsync();

            using (var client = new HttpClient())
            {
                try
                {
                    var data = await client.GetStringAsync("http://localhost:8877");

                    Assert.IsNotEmpty(data);
                }
                catch (HttpRequestException)
                {
                    Assert.Ignore("Linux");
                }
            }

            EndPointManager.UseIpv6 = false;
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
                var data = await client.GetStringAsync("http://[::1]:8877");

                Assert.IsNotEmpty(data);
            }
        }
    }
}
