namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using Swan.Formatters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

    [TestFixture]
    public class WebApiModuleTest
    {
        protected WebServer WebServer;
        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            WebServer = new WebServer(WebServerUrl)
                .WithWebApiController<TestController>();

            WebServer.RunAsync();
        }

        internal class FormDataSample
        {
            public string test { get; set; }
            public List<string> id { get; set; }
        }



        [TearDown]
        public void Kill()
        {
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            WebServer.Dispose();
        }

        public class HttpGet : WebApiModuleTest
        {
            [Test]
            public async Task JsonData()
            {
                List<Person> remoteList;

                var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + TestController.GetPath);

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var jsonBody = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    Assert.IsNotNull(jsonBody, "Json Body is not null");
                    Assert.IsNotEmpty(jsonBody, "Json Body is empty");

                    remoteList = Json.Deserialize<List<Person>>(jsonBody);

                    Assert.IsNotNull(remoteList, "Json Object is not null");
                    Assert.AreEqual(remoteList.Count, PeopleRepository.Database.Count, "Remote list count equals local list");
                }

                await TestHelper.ValidatePerson(WebServerUrl + TestController.GetPath + remoteList.First().Key);
            }

            [Test]
            public async Task JsonDataWithMiddleUrl()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestController.GetMiddlePath.Replace("*", person.Key.ToString()));
            }

            [Test]
            public async Task JsonAsyncData()
            {
                var person = PeopleRepository.Database.First();
                await TestHelper.ValidatePerson(WebServerUrl + TestController.GetAsyncPath + person.Key);
            }
        }
        
        public class HttpPost : WebApiModuleTest
        {
            [Test]
            public async Task JsonData()
            {
                using (var client = new HttpClient())
                {
                    var model = new Person() { Key = 10, Name = "Test" };
                    var payloadJson = new StringContent(Json.Serialize(model), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(WebServerUrl + TestController.GetPath, payloadJson);

                    var result = Json.Deserialize<Person>(await response.Content.ReadAsStringAsync());
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.Name, model.Name);
                }
            }
        }

        public class FormData : WebApiModuleTest
        {
            [TestCase("id", "id")]
            [TestCase("id[0]", "id[1]")]
            public async Task MultipleIndexedValues(string label1, string label2)
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[] {
                    new KeyValuePair<string, string>("test", "data"),
                    new KeyValuePair<string, string>(label1, "1"),
                    new KeyValuePair<string, string>(label2, "2")
                };

                    var formContent = new FormUrlEncodedContent(content);

                    var result = await webClient.PostAsync(WebServerUrl + TestController.EchoPath, formContent);
                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<FormDataSample>(data);
                    Assert.IsNotNull(obj);
                    Assert.AreEqual(content.First().Value, obj.test);
                    Assert.AreEqual(2, obj.id.Count);
                    Assert.AreEqual(content.Last().Value, obj.id.Last());
                }
            }

            [Test]
            public async Task TestDictionaryFormData()
            {
                using (var webClient = new HttpClient())
                {
                    var content = new[]
                    {
                    new KeyValuePair<string, string>("test", "data"),
                    new KeyValuePair<string, string>("id", "1")
                };

                    var formContent = new FormUrlEncodedContent(content);

                    var result = await webClient.PostAsync(WebServerUrl + TestController.EchoPath, formContent);
                    Assert.IsNotNull(result);
                    var data = await result.Content.ReadAsStringAsync();
                    var obj = Json.Deserialize<Dictionary<string, string>>(data);
                    Assert.AreEqual(2, obj.Keys.Count);

                    Assert.AreEqual(content.First().Key, obj.First().Key);
                    Assert.AreEqual(content.First().Value, obj.First().Value);
                }
            }
        }
        
        [Test]
        public async Task WebApiWithConstructor()
        {
            const string name = "Test";

            WebServer.Module<WebApiModule>().RegisterController(() => new TestControllerWithConstructor(name));

            var request = (HttpWebRequest)WebRequest.Create(WebServerUrl + "name");

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                var body = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Assert.AreEqual(body, name);
            }
        }
    }
}