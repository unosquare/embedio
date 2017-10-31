using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class WildcardRoutingTest
    {
        protected WebServer WebServer;
        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            // using default wildcard
            WebServer = new WebServer(WebServerUrl);
            WebServer.RegisterModule(new TestRoutingModule());
            WebServer.RunAsync();
        }

        [TearDown]
        public void Kill()
        {
            WebServer.Dispose();
        }

        public class GetData : WildcardRoutingTest
        {
            [Test]
            public async Task WithoutWildcard()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}empty");

                Assert.AreEqual("data", call);
            }

            [Test]
            public async Task WithWildcard()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}data/1");

                Assert.AreEqual("1", call);
            }


            [Test]
            public async Task MultipleWildcard()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}data/1/time");

                Assert.AreEqual("time", call);
            }
        }
    }
}
