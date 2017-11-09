﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Net.Http;
    using Constants;
    using NUnit.Framework;
    using Swan.Formatters;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class RegexWebApiModuleTest
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

        [TearDown]
        public void Kill()
        {
            WebServer.Dispose();
        }

        public class GetJsonData : RegexWebApiModuleTest
        {
            [Test]
            public async Task WithoutRegex()
            {
                var http = new HttpClient();
                var jsonString = await http.GetStringAsync(WebServerUrl + TestRegexController.RelativePath + "empty");

                Assert.IsNotEmpty(jsonString);
            }

            [Test]
            public async Task WithRegexId()
            {
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regex/1");
            }

            [Test]
            public async Task WithOptRegexId()
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
            public async Task AsyncWithRegexId()
            {
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexAsync/1");
            }

            [Test]
            public async Task WithRegexDate()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regexdate/" +
                               person.DoB.ToString("yyyy-MM-dd"));
            }

            [Test]
            public async Task WithRegexWithTwoParams()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestRegexController.RelativePath + "regextwo/" +
                               person.MainSkill + "/" + person.Age);
            }
        }
    }
}
