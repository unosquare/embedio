namespace Unosquare.Labs.EmbedIO.Tests
{
    using Swan.Formatters;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

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
            WebServer =
                new WebServer(WebServerUrl, RoutingStrategy.Regex)
                    .WithWebApiController<TestRegexController>();
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebApi()
        {
            Assert.IsNotNull(WebServer.Module<WebApiModule>(), "WebServer has WebApiModule");

            Assert.AreEqual(WebServer.Module<WebApiModule>().ControllersCount, 1, "WebApiModule has one controller");
        }
        
        [Test]
        public async Task GetJsonDataWithRegexId()
        {
            await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regex/1");
        }

        [Test]
        public async Task GetJsonDataWithOptRegexId()
        {
            // using null value
            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestRegexController.RelativePath + "regexopt");

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNull(jsonBody, "Json Body is not null");
                Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

                var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count, "Remote list count equals local list");
            }

            // using a value
            await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexopt/1");
        }

        [Test]
        public async Task GetJsonDatAsyncWithRegexId()
        {
            await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexAsync/1");
        }

        [Test]
        public async Task GetJsonDataWithRegexDate()
        {
            var person = PeopleRepository.Database.First();
            await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexdate/" +
                           person.DoB.ToString("yyyy-MM-dd"));
        }

        [Test]
        public async Task GetJsonDataWithRegexWithTwoParams()
        {
            var person = PeopleRepository.Database.First();
            await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regextwo/" +
                           person.MainSkill + "/" + person.Age);
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            WebServer.Dispose();
        }
    }
}
