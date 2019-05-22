﻿using EmbedIO.Constants;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class RegexWebApiModuleTest : PersonFixtureBase
    {
        public RegexWebApiModuleTest()
            : base(ws => ws.WithWebApiController<TestController>(), true)
        {
        }

        public class GetJsonData : RegexWebApiModuleTest
        {
            [Test]
            public async Task WithoutRegex_ReturnsOk()
            {
                var jsonString = await GetString($"{TestController.RelativePath}empty");

                Assert.IsNotEmpty(jsonString);
            }

            [Test]
            public async Task BigData_ReturnsOk()
            {
                var jsonString = await GetString($"{TestController.RelativePath}big");

                Assert.IsNotEmpty(jsonString);
                Assert.IsTrue(jsonString.StartsWith("["));
                Assert.IsTrue(jsonString.EndsWith("]"));
            }

            [Test]
            public async Task WithRegexId_ReturnsOk()
            {
                await ValidatePerson($"{TestController.RelativePath}regex/1");
            }

            [Test]
            public async Task WithOptRegexIdAndValue_ReturnsOk()
            {
                await ValidatePerson(TestController.RelativePath + "regexopt/1");
            }

            [Test]
            public async Task WithOptRegexIdAndNonValue_ReturnsOk()
            {
                var jsonBody = await GetString(TestController.RelativePath + "regexopt");
                var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                Assert.AreEqual(
                    remoteList.Count,
                    PeopleRepository.Database.Count,
                    "Remote list count equals local list");
            }

            [Test]
            public async Task AsyncWithRegexId_ReturnsOk()
            {
                await ValidatePerson(TestController.RelativePath + "regexAsync/1");
            }

            [Test]
            public async Task WithRegexDate_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson(TestController.RelativePath + "regexdate/" +
                                                person.DoB.ToString("yyyy-MM-dd"));
            }

            [Test]
            public async Task WithRegexWithTwoParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson(TestController.RelativePath + "regextwo/" +
                                                person.MainSkill + "/" + person.Age);
            }

            [Test]
            public async Task WithRegexWithOptionalParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();

                await ValidatePerson(TestController.RelativePath + "regexthree/" +
                                     person.MainSkill);
            }
        }

        public class Http405 : RegexWebApiModuleTest
        {
            [Test]
            public async Task ValidWebApiPathInvalidMethod_Returns405()
            {
                var request = new TestHttpRequest(WebServerUrl + TestController.RelativePath + "regex/1", HttpVerbs.Delete);

                var response = await SendAsync(request);

                Assert.AreEqual((int)HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }
    }
}