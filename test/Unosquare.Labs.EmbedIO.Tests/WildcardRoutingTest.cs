using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class WildcardRoutingTest : FixtureBase
    {
        public WildcardRoutingTest()
            : base(ws => ws.RegisterModule(new TestRoutingModule()))
        {
            // placeholder    
        }

        [Test]
        public async Task WithoutWildcard()
        {
            using (var client = new HttpClient())
            {
                var call = await client.GetStringAsync($"{WebServerUrl}empty");

                Assert.AreEqual("data", call);
            }
        }

        [Test]
        public async Task WithWildcard()
        {
            using (var client = new HttpClient())
            {
                var call = await client.GetStringAsync($"{WebServerUrl}data/1");

                Assert.AreEqual("1", call);
            }
        }


        [Test]
        public async Task MultipleWildcard()
        {
            using (var client = new HttpClient())
            {
                var call = await client.GetStringAsync($"{WebServerUrl}data/1/time");

                Assert.AreEqual("time", call);
            }
        }
    }
}