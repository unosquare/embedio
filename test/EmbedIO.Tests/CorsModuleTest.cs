using System.Net;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class CorsModuleTest : FixtureBase
    {
        private static readonly object TestObj = new { Message = "OK" };

        public CorsModuleTest()
            : base(
                ws =>
                {
                    ws.WithCors(
                        "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                        "content-type",
                        "post,get");

                    var webModule = new WebApiModule("/");
                    webModule.RegisterController<TestController>();

                    ws.Modules.Add(nameof(WebApiModule), webModule);
                    ws.Modules.Add(nameof(ActionModule), new ActionModule((ctx, path, ct) => ctx.JsonResponseAsync(TestObj, ct)));
                },
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestFallback_ReturnsJsonObject()
        {
            var jsonBody = await GetString("invalidpath");

            Assert.AreEqual(Json.Serialize(TestObj), jsonBody, "Same content");
        }

        [Test]
        public async Task RequestOptionsVerb_ReturnsOK()
        {
            var request = new TestHttpRequest(WebServerUrl + TestController.GetPath, HttpVerbs.Options);
            request.Headers.Add(HttpHeaderNames.Origin, "http://unosquare.github.io");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestMethod, "post");
            request.Headers.Add(HttpHeaderNames.AccessControlRequestHeaders, "content-type");

            var response = await SendAsync(request);
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }
    }
}