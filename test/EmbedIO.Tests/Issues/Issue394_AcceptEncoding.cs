using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue394_AcceptEncoding
    {
        [Test]
        public async Task ActionModule_Handle_AcceptEncodingProperly()
        {
            const string DefaultUrl = "http://localhost:1234/";

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server.WithAction("/", HttpVerb.Get, async context => { await context.SendDataAsync(12345); });

            _ = server.RunAsync();

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflat"));
            using var response = await client.GetAsync(DefaultUrl).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual("12345", responseString);
        }
    }
}
