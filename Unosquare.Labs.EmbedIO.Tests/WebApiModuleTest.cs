using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Unosquare.Labs.EmbedIO.Tests.Properties;

    [TestFixture]
    public class WebApiModuleTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.ServerAddress, Logger).WithWebApiController<TestController>();
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebApi()
        {
            Assert.IsNotNull(WebServer.Module<WebApiModule>(), "WebServer has WebApiModule");

            Assert.AreEqual(WebServer.Module<WebApiModule>().ControllersCount, 1, "WebApiModule has one controller");
        }

        [Test]
        public void GetJsonData()
        {
            List<TestController.Person> remoteList = null;

            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + TestController.GetPath);

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                remoteList = JsonConvert.DeserializeObject<List<TestController.Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, TestController.People.Count, "Remote list count equals local list");
            }

            var singleRequest =
                (HttpWebRequest)
                    WebRequest.Create(Resources.ServerAddress + TestController.GetPath + remoteList.First().Key);

            using (var response = (HttpWebResponse) singleRequest.GetResponse())
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

        [Test]
        public void PostJsonData()
        {
            var model = new TestController.Person() {Key = 10, Name = "Test"};
            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + TestController.GetPath);
            request.Method = "POST";

            using (var dataStream = request.GetRequestStream())
            {
                var byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Assert.IsNotNullOrEmpty(jsonString);

                var json = JsonConvert.DeserializeObject<TestController.Person>(jsonString);
                Assert.IsNotNull(json);
                Assert.AreEqual(json.Name, model.Name);
            }
        }

        [Test]
        public void TestWebApiWithConstructor()
        {
            const string name = "Test";

            WebServer.Module<WebApiModule>().RegisterController(() => new TestControllerWithConstructor(name));

            var request = (HttpWebRequest) WebRequest.Create(Resources.ServerAddress + "name");

            using (var response = (HttpWebResponse) request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, name);
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