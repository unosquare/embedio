using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue389_RequestContentLength
    {
        [Test]
        public async Task ActionModuleReadsProperty_Handle_ContentLengthProperly()
        {
            const string DefaultUrl = "http://localhost:1234/";
            const string Content = "content";

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server.WithAction("/", HttpVerbs.Post, async context => {
                await context.SendDataAsync(context.Request.ContentLength64);
            });

            _ = server.RunAsync();

            using var client = new HttpClient();
            using var content = new StringContent(Content, Encoding.UTF8, "text/plain");
            using var response = await client.PostAsync(DefaultUrl, content).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual(Content.Length.ToString(), responseString);
        }

        [Test]
        public async Task ActionModuleReadsKey_Handle_ContentLengthProperly()
        {
            const string DefaultUrl = "http://localhost:1234/";
            const string Content = "content";

            using var server = new WebServer(HttpListenerMode.EmbedIO, DefaultUrl);
            server.WithAction("/", HttpVerbs.Post, async context => {
                await context.SendDataAsync(context.Request.Headers[HttpHeaderNames.ContentLength]);
            });

            _ = server.RunAsync();

            using var client = new HttpClient();
            using var content = new StringContent(Content, Encoding.UTF8, "text/plain");
            using var response = await client.PostAsync(DefaultUrl, content).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.AreEqual(Content.Length.ToString(), responseString);
        }
    }
}
