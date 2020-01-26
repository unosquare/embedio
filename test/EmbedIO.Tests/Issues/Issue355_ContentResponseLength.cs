using NUnit.Framework;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    public class Issue355_ContentResponseLength
    {
        [Test]
        public async Task ActionModuleWithProperty_Handle_ContentLengthProperly()
        {
            const string DefaultUrl = "http://localhost:1234/";

            var ok = Encoding.UTF8.GetBytes("content");

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server.WithAction("/", HttpVerb.Get, async context =>
            {
                context.Response.ContentLength64 = ok.Length;

                await context.Response.OutputStream.WriteAsync(ok, 0, ok.Length);
            });

            _ = server.RunAsync();

            using var client = new HttpClient();
            using var response = await client.GetAsync(DefaultUrl).ConfigureAwait(false);
            var responseArray = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Assert.AreEqual(ok[0], responseArray[0]);
        }

        [Test]
        public async Task ActionModuleWithHeaderCollection_Handle_ContentLengthProperly()
        {
            var ok = Encoding.UTF8.GetBytes("content");

            using var server = new WebServer(1234);
            server.WithAction("/", HttpVerb.Get, async context =>
            {
                context.Response.Headers[HttpHeaderNames.ContentLength] = ok.Length.ToString();

                await context.Response.OutputStream.WriteAsync(ok, 0, ok.Length);
            });

            _ = server.RunAsync();

            using var client = new HttpClient();
            using var response = await client.GetAsync("http://localhost:1234/").ConfigureAwait(false);
            var responseArray = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Assert.AreEqual(ok[0], responseArray[0]);
        }
    }
}