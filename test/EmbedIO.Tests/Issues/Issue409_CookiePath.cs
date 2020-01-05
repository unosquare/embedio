using EmbedIO.Routing;
using EmbedIO.WebApi;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    public class Issue409_CookiePath
    {
        [Test]
        public async Task SessionModule_Handle_SameCookieBetweenModules()
        {
            const string DefaultUrl = "http://localhost:1234/";

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server
                .WithLocalSessionManager()
                .WithWebApi("/v1", o => o.WithController<CookieController>())
                .WithWebApi("/v2", o => o.WithController<CookieController>());

            _ = server.RunAsync();

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
            };

            using var client = new HttpClient(handler);

            using var responseOne = await client.GetAsync($"{DefaultUrl}v1/test").ConfigureAwait(false);
            var responseOneString = await responseOne.Content.ReadAsStringAsync().ConfigureAwait(false);

            using var responseTwo = await client.GetAsync($"{DefaultUrl}v2/test").ConfigureAwait(false);
            var responseTwoString = await responseTwo.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual(responseOneString, responseTwoString);
        }

        private class CookieController : WebApiController
        {
            [Route(HttpVerbs.Get, "/test")]
            public async Task Get()
            {
                Session["key"] = 1;
                await HttpContext.SendDataAsync(Session.Id);
            }
        }
    }
}
