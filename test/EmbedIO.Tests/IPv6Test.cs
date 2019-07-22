using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Net;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class IPv6Test
    {
        [SetUp]
        public void Setup()
        {
            EndPointManager.UseIpv6 = true;

            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
        }

        [TestCase("http://[::1]:8877")]
        [TestCase("http://127.0.0.1:8877")]
        public async Task WithUseIpv6_ReturnsValid(string urlTest)
        {
            if (Runtime.OS != Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://*:8877");
            instance.OnAny(ctx => ctx.SendDataAsync(DateTime.Now));

            var dump = instance.RunAsync();

            using (var client = new HttpClient())
            {
                Assert.IsNotEmpty(await client.GetStringAsync(urlTest));
            }
        }

        [Test]
        public async Task WithIpv6Loopback_ReturnsValid()
        {
            if (Runtime.OS != Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var instance = new WebServer(HttpListenerMode.EmbedIO, "http://[::1]:8877");
            instance.OnAny(ctx => ctx.SendDataAsync(DateTime.Now));

            var dump = instance.RunAsync();

            using (var client = new HttpClient())
            {
                Assert.IsNotEmpty(await client.GetStringAsync("http://[::1]:8877"));
            }
        }

        [TearDown]
        public void TearDown()
        {
            EndPointManager.UseIpv6 = false;
        }
    }
}
