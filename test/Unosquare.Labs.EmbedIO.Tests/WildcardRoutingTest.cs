using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Constants;
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

        [Test]
        public async Task GetDataWithWildcard()
        {
            var client = new HttpClient();

            var call1 = await client.GetStringAsync($"{WebServerUrl}data/1");

            Assert.AreEqual("data", call1);

            var call2 = await client.GetStringAsync($"{WebServerUrl}data/1/asdasda/dasdasasda");

            Assert.AreEqual("data", call2);
        }

        [TearDown]
        public void Kill()
        {
            WebServer.Dispose();
        }

    }
}
