using NUnit.Framework;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class RegexRoutingTest : FixtureBase
    {
        public RegexRoutingTest()
            : base(ws => ws.WithModule(new TestRegexModule("/")), true)
        {
        }

        public class GetData : RegexRoutingTest
        {
            [Test]
            public async Task GetDataWithoutRegex()
            {
                var call = await GetString("empty");

                Assert.AreEqual(string.Empty, call);
            }

            [Test]
            public async Task GetDataWithRegex()
            {
                var call = await GetString("data/1");

                Assert.AreEqual("1", call);
            }

            [Test]
            public async Task GetDataWithMultipleRegex()
            {
                var call = await GetString("data/1/2");

                Assert.AreEqual("2", call);
            }
        }
    }
}