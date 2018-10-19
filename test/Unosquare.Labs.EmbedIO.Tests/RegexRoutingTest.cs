namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class RegexRoutingTest : FixtureBase
    {
        public RegexRoutingTest()
            : base(ws => ws.RegisterModule(new TestRegexModule()), Constants.RoutingStrategy.Regex, false)
        {
        }

        public class GetData : RegexRoutingTest
        {
            [Test]
            public async Task GetDataWithoutRegex()
            {
                var call = await GetString($"{WebServerUrl}empty");

                Assert.AreEqual("data", call);
            }

            [Test]
            public async Task GetDataWithRegex()
            {
                var call = await GetString($"{WebServerUrl}data/1");

                Assert.AreEqual("1", call);
            }

            [Test]
            public async Task GetDataWithMultipleRegex()
            {
                var call = await GetString($"{WebServerUrl}data/1/dasdasasda");

                Assert.AreEqual("dasdasasda", call);
            }
        }
    }
}