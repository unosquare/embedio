﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using Swan.Formatters;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class CorsModuleTest : FixtureBase
    {
        private static readonly object TestObj = new {Message = "OK"};

        public CorsModuleTest()
            : base(ws =>
            {
                ws.EnableCors(
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                    "content-type",
                    "post,get");

                ws.RegisterModule(new WebApiModule());
                ws.Module<WebApiModule>().RegisterController<TestController>();
                ws.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse(TestObj)));
            }, RoutingStrategy.Wildcard)
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
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Options, WebServerUrl + TestController.GetPath);
                request.Headers.Add(Headers.Origin, "http://unosquare.github.io");
                request.Headers.Add(Headers.AccessControlRequestMethod, "post");
                request.Headers.Add(Headers.AccessControlRequestHeaders, "content-type");

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                }
            }
        }
    }
}