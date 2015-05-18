namespace Unosquare.Labs.EmbedIO.Tests
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class WebApiModuleTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.ServerAddress, Logger);
            WebServer.RegisterModule(new WebApiModule());
            WebServer.Module<WebApiModule>().RegisterController<TestController>();
            WebServer.RunAsync();
        }

        [Test]
        public void GetJsonData()
        {
            List<TestController.Person> remoteList = null;

            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + TestController.GetPath);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                remoteList = JsonConvert.DeserializeObject <List<TestController.Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, TestController.People.Count, "Remote list count equals local list");
            }

            var singleRequest = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + TestController.GetPath + remoteList.First().Key);

            using (var response = (HttpWebResponse)singleRequest.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                var item = JsonConvert.DeserializeObject<TestController.Person>(jsonBody);

                Assert.IsNotNull(item, "Json Object is not null");
                Assert.AreEqual(item.Name, remoteList.First().Name, "Remote objects equality");
                Assert.AreEqual(item.Name, TestController.People.First().Name, "Remote and local objects equality");
            }
        }

        // TODO: Test POST

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}
