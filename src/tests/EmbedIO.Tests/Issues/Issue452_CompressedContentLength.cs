using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    [TestFixture]
    public class Issue452_CompressedContentLength : FileModuleTest
    {
        [Test]
        public async Task GetCompressedFile_Succeeds()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, UrlPath.Root);

            // Force server to use gzip compression, in order to trigger issue #452
            request.Headers.AcceptEncoding.Clear();
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using var response = await Client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }
    }
}