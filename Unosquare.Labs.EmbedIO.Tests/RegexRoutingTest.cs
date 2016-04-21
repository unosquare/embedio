using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class RegexRoutingTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer =
                new WebServer(Resources.ServerAddress, Logger, RoutingStrategy.Regex)
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
        public void GetJsonDataWithRegexId()
        {
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regex/1");
        }

        [Test]
        public void GetJsonDataWithOptRegexId()
        {
            // using null value
            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress + TestRegexController.RelativePath + "regexopt");

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                var remoteList = JsonConvert.DeserializeObject<List<Person>>(jsonBody);

                Assert.IsNotNull(remoteList, "Json Object is not null");
                Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count, "Remote list count equals local list");
            }

            // using a value
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexopt/1");
        }

        [Test]
        public void GetJsonDatAsyncaWithRegexId()
        {
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexasync/1");
        }

        [Test]
        public void GetJsonDataWithRegexDate()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexdate/" +
                           person.DoB.ToString("yyyy-MM-dd"));
        }

        [Test]
        public void GetJsonDataWithRegexWithTwoParams()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regextwo/" +
                           person.MainSkill + "/" + person.Age);
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}
