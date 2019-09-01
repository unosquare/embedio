using EmbedIO.Testing;
using NUnit.Framework;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    public class Issue355_ContentResponseLength
    {
        [Test]
        public Task ActionModule_Handle_ContentLengthProperly()
        {
            var ok = Encoding.UTF8.GetBytes("content");

            void Configure(IWebServer server) => server
                .WithAction("/", HttpVerbs.Get, async context =>
                {
                    context.Response.Headers[HttpHeaderNames.ContentLength] = ok.Length.ToString();
                    await context.Response.OutputStream.WriteAsync(ok, 0, ok.Length);
                });

            async Task Use(HttpClient client)
            {
                using (var response = await client.GetAsync($"/").ConfigureAwait(false))
                {
                    var responseArray = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                    Assert.AreEqual(ok[0], responseArray[0]);
                }
            }

            return TestWebServer.UseAsync(Configure, Use);
        }
    }
}
