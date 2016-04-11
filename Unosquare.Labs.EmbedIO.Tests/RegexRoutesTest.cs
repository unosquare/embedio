using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class RegexRoutesTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer =
                new WebServer(Resources.ServerAddress, Logger, RoutingStrategyEnum.Regex)
                    .WithWebApiController<TestRegexController>();
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebApi()
        {
            Assert.IsNotNull(WebServer.Module<WebApiModule>(), "WebServer has WebApiModule");

            Assert.AreEqual(WebServer.Module<WebApiModule>().ControllersCount, 1, "WebApiModule has one controller");
        }

        private static void ValidatePerson(string url)
        {
            var person = PeopleRepository.Database.First();

            var singleRequest = (HttpWebRequest) WebRequest.Create(url);

            using (var response = (HttpWebResponse) singleRequest.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.IsNotNullOrEmpty(jsonBody, "Json Body is not null or empty");

                var item = JsonConvert.DeserializeObject<Person>(jsonBody);

                Assert.IsNotNull(item, "Json Object is not null");
                Assert.AreEqual(item.Name, person.Name, "Remote objects equality");
                Assert.AreEqual(item.Name, PeopleRepository.Database.First().Name, "Remote and local objects equality");
            }
        }

        [Test]
        public void GetJsonDataWithRegexId()
        {
            ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regex/1");
        }

        [Test]
        public void GetJsonDataWithRegexDate()
        {
            var person = PeopleRepository.Database.First();
            ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexdate/" +
                           person.DoB.ToString("yyyy-MM-dd"));
        }

        [Test]
        public void GetJsonDataWithRegexWithTwoParams()
        {
            var person = PeopleRepository.Database.First();
            ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regextwo/" +
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