using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using EmbedIO.WebApi;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class WebApiModuleTest : PersonEndToEndFixtureBase
    {
        public WebApiModuleTest()
            : base(ws => ws.WithWebApi("/api", m => m.WithController<TestController>()))
        {
        }

        public class HttpGet : WebApiModuleTest
        {
            [Test]
            public async Task EmptyResponse_ReturnsOk()
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(WebServerUrl + "/api/empty");

                    Assert.IsNotNull(response);
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        public class HttpPost : WebApiModuleTest
        {
            [Test]
            public async Task JsonData_ReturnsOk()
            {
                using (var client = new HttpClient())
                {
                    var model = new Person { Key = 10, Name = "Test" };
                    var payloadJson = new StringContent(
                        Json.Serialize(model),
                        System.Text.Encoding.UTF8,
                        MimeType.Json);

                    var response = await client.PostAsync(WebServerUrl + "/api/regex", payloadJson);

                    var result = Json.Deserialize<Person>(await response.Content.ReadAsStringAsync());
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.Name, model.Name);
                }
            }
        }

        public class Http405 : WebApiModuleTest
        {
            [Test]
            public async Task ValidPathInvalidMethod_Returns405()
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, WebServerUrl + "/api/regex");

                    var response = await client.SendAsync(request);

                    Assert.AreEqual(response.StatusCode, HttpStatusCode.MethodNotAllowed);
                }
            }
        }

        public class QueryData : WebApiModuleTest
        {
            [Test]
            public async Task QueryDataAttribute_ReturnsCorrectValues()
            {
                using (var webClient = new HttpClient())
                {
                    var result = await webClient.GetAsync($"{WebServerUrl}/api/{TestController.QueryTestPath}?a=first&one=1&a=second&two=2&none&equal=&a[]=third");
                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var dict = Json.Deserialize<Dictionary<string, object>>(data);
                    Assert.IsNotNull(dict);

                    Assert.AreEqual("1", dict["one"]);
                    Assert.AreEqual("2", dict["two"]);
                    Assert.AreEqual(string.Empty, dict["none"]);
                    Assert.AreEqual(string.Empty, dict["equal"]);
                    Assert.Throws<KeyNotFoundException>(() => {
                        var three = dict["three"];
                    });

                    var a = dict["a"] as IEnumerable<object>;
                    Assert.NotNull(a);
                    var list = a.Cast<string>().ToList();
                    Assert.AreEqual(3, list.Count);
                    Assert.AreEqual("first", list[0]);
                    Assert.AreEqual("second", list[1]);
                    Assert.AreEqual("third", list[2]);
                }
            }

            [Test]
            public async Task QueryFieldAttribute_ReturnsCorrectValue()
            {
                using (var webClient = new HttpClient())
                {
                    var value = Guid.NewGuid().ToString();
                    var result = await webClient.GetAsync($"{WebServerUrl}/api/{TestController.QueryFieldTestPath}?id={value}");
                    Assert.IsNotNull(result);
                    var returnedValue = await result.Content.ReadAsStringAsync();
                    Assert.AreEqual(value, returnedValue);
                }
            }
        }

        public class FormData : WebApiModuleTest
        {
            [TestCase("Id", "Id")]
            [TestCase("Id[0]", "Id[1]")]
            public async Task MultipleIndexedValues_ReturnsOk(string label1, string label2)
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[]
                    {
                        new KeyValuePair<string, string>("Test", "data"),
                        new KeyValuePair<string, string>(label1, "1"),
                        new KeyValuePair<string, string>(label2, "2"),
                    };

                    var formContent = new FormUrlEncodedContent(content);

                    var result = await webClient.PostAsync($"{WebServerUrl}/api/{TestController.EchoPath}", formContent);
                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<FormDataSample>(data);
                    Assert.IsNotNull(obj);
                    Assert.AreEqual(content.First().Value, obj.Test);
                    Assert.AreEqual(2, obj.Id.Count);
                    Assert.AreEqual(content.Last().Value, obj.Id.Last());
                }
            }

            [Test]
            public async Task TestDictionaryFormData_ReturnsOk()
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[]
                    {
                        new KeyValuePair<string, string>("Test", "data"),
                        new KeyValuePair<string, string>("Id", "1"),
                    };

                    var formContent = new FormUrlEncodedContent(content);

                    var result =
                        await webClient.PostAsync(WebServerUrl + "/api/" + TestController.EchoPath, formContent);

                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<Dictionary<string, string>>(data);
                    Assert.AreEqual(2, obj.Keys.Count);

                    Assert.AreEqual(content.First().Key, obj.First().Key);
                    Assert.AreEqual(content.First().Value, obj.First().Value);
                }
            }
        }

        internal class FormDataSample
        {
            public string Test { get; set; }
            public List<string> Id { get; set; }
        }

        public class GetJsonData : WebApiModuleTest
        {
            [Test]
            public async Task WithRegexId_ReturnsOk()
            {
                await ValidatePerson("/api/regex/1");
            }

            [Test]
            public async Task WithOptRegexIdAndValue_ReturnsOk()
            {
                await ValidatePerson("/api/regexopt/1");
            }

            [Test]
            public async Task WithOptRegexIdAndNonValue_ReturnsOk()
            {
                var jsonBody = await GetString("/api/regexopt");
                var remoteList = Json.Deserialize<List<Person>>(jsonBody);

                Assert.AreEqual(
                    remoteList.Count,
                    PeopleRepository.Database.Count,
                    "Remote list count equals local list");
            }

            [Test]
            public async Task WithRegexDate_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson($"/api/regexdate/{person.DoB:yyyy-MM-dd}");
            }

            [Test]
            public async Task WithRegexWithTwoParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();
                await ValidatePerson($"/api/regextwo/{person.MainSkill}/{person.Age}");
            }

            [Test]
            public async Task WithRegexWithOptionalParams_ReturnsOk()
            {
                var person = PeopleRepository.Database.First();

                await ValidatePerson($"/api/regexthree/{person.MainSkill}");
            }
        }
    }
}