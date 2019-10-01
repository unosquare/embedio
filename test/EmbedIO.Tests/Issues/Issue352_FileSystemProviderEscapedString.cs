using EmbedIO.Testing;
using System.Net.Http;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EmbedIO.Tests.Issues
{
    public class Issue352_FileSystemProviderEscapedString
    {
        [Test]
        public Task FileSystemProvider_Handle_PathWithEscapedString()
        {
            const string ok = "Content";

            var tempFolder = $"Folder {DateTime.Now.Ticks}";
            var tempPathWithWhitespace = Path.Combine(Path.GetTempPath(), tempFolder);
            Directory.CreateDirectory(tempPathWithWhitespace);
            var tempFile = Path.Combine(tempPathWithWhitespace, "index.html");
            File.WriteAllText(tempFile, ok);

            void Configure(IWebServer server) => server
                .WithStaticFolder("/", Path.GetTempPath(), true);

            async Task Use(HttpClient client)
            {
                using var response = await client.GetAsync($"/{tempFolder}").ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.AreEqual(ok, responseString);
            }

            return TestWebServer.UseAsync(Configure, Use);
        }
    }
}
