using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class RegexRoutingTest
    {
        protected WebServer WebServer;
        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            // using default wildcard
            WebServer = new WebServer(WebServerUrl, Constants.RoutingStrategy.Regex);
            WebServer.RegisterModule(new TestRoutingModule());
            WebServer.RunAsync();
        }

        [TearDown]
        public void Kill()
        {
            WebServer.Dispose();
        }

        public class GetData : RegexRoutingTest
        {
            [Test]
            public async Task GetDataWithoutRegex()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}empty");

                Assert.AreEqual("data", call);
            }

            [Test]
            public async Task GetDataWithRegex()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}data/1");

                Assert.AreEqual("1", call);
            }
            
            [Test]
            public async Task GetDataWithMultipleRegex()
            {
                var client = new HttpClient();

                var call = await client.GetStringAsync($"{WebServerUrl}data/1/asdasda/dasdasasda");

                Assert.AreEqual("dasdasasda", call);
            }
        }
    }
}
