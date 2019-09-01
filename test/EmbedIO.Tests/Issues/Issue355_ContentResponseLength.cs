using NUnit.Framework;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    public class Issue355_ContentResponseLength
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task ActionModule_Handle_ContentLengthProperly(bool useProperty)
        {
            var ok = Encoding.UTF8.GetBytes("content");

            using (var server = new WebServer(1234))
            {
                server.WithAction("/", HttpVerbs.Get, async context =>
                {
                    if (useProperty)
                        context.Response.ContentLength64 = ok.Length;
                    else
                        context.Response.Headers[HttpHeaderNames.ContentLength] = ok.Length.ToString();

                    await context.Response.OutputStream.WriteAsync(ok, 0, ok.Length);
                });

                server.RunAsync();

                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync("http://localhost:1234/").ConfigureAwait(false))
                    {
                        var responseArray = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                        Assert.AreEqual(ok[0], responseArray[0]);
                    }
                }
            }
        }
    }
}
