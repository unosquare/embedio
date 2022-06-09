using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    [TestFixtureSource(nameof(FixtureArgs))]
    public class Issue531_DefaultPort : EndToEndFixtureBase
    {
        private const string TestString = "This is a test.";
        private static readonly IEnumerable FixtureArgs = new[] { false, true };

        public Issue531_DefaultPort(bool useIPv6)
            : base(true, useIPv6)
        {
        }

        protected override void OnSetUp()
        {
            Server.WithAction("/", HttpVerbs.Get, context => context.SendStringAsync(TestString, MimeType.PlainText, WebServer.DefaultEncoding));
        }

        [Test]
        public async Task DefaultPort_Ok()
        {
            var responseString = await Client.GetStringAsync(WebServerUrl);
            Assert.AreEqual(TestString, responseString);
        }
    }
}