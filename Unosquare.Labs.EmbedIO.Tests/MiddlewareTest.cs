using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class MiddlewareTest
    {
        protected TestMiddleware Middleware = new TestMiddleware();
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RunAsync(app: Middleware);
        }

        [Test]
        public void GetIndex()
        {
            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(jsonString, "{\"Status\":\"OK\"}", "Same JSON content");
            }
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}