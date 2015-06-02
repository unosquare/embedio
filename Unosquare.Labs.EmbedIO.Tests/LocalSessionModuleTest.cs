using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class LocalSessionModuleTest
    {
        protected string RootPath;
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            RootPath = TestHelper.SetupStaticFolder();

            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RegisterModule(new LocalSessionModule());
            WebServer.RegisterModule(new StaticFilesModule(RootPath));
            WebServer.RunAsync();
        }

        [Test]
        public void HasSessionModule()
        {
            Assert.IsNotNull(WebServer.SessionModule, "Session module is not null");
            Assert.AreEqual(WebServer.SessionModule.Handlers.Count, 1, "Session module has one handler");
        }

        [Test]
        public void GetCookie()
        {
            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress);
            request.CookieContainer = new CookieContainer();

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                Assert.IsNotNull(response.Cookies, "Cookies are not null");
                Assert.Greater(response.Cookies.Count, 0, "Cookies are not empty");
                Assert.IsNotNull(response.Cookies[0], "Cookies has one cookie");

                var content = response.Cookies[0].Value;

                Assert.IsNotNullOrEmpty(content, "Cookie content is not null");
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