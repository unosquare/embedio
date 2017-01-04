namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class MiddlewareTest
    {
        protected TestMiddleware Middleware = new TestMiddleware();
        protected WebServer WebServer;
        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            WebServer = new WebServer(WebServerUrl);
            WebServer.RunAsync(app: Middleware);
        }

        [Test]
        public async Task GetIndex()
        {
            var request = (HttpWebRequest) WebRequest.Create(WebServerUrl);

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual("{\"Status\": \"OK\"}", jsonString, "Same JSON content");
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