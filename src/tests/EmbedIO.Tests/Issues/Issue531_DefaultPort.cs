using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    [TestFixture]
    public class Issue531_DefaultPort : FileModuleTest
    {
        [Test]
        public async Task DefaultPort_Ok()
        {
            const string DefaultUrl = "http://localhost/";

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server.WithAction("/", HttpVerb.Get, async context => { await context.SendDataAsync(12345); });

            _ = server.RunAsync();

            using var client = new HttpClient();
            using var response = await client.GetAsync(DefaultUrl).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual("12345", responseString);
        }
    }
}