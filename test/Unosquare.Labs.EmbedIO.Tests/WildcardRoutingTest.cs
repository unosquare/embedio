namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class WildcardRoutingTest : FixtureBase
    {
        public WildcardRoutingTest()
            : base(ws => ws.RegisterModule(new TestRoutingModule()), Constants.RoutingStrategy.Wildcard)
        {
            // placeholder    
        }

        [Test]
        public async Task WithoutWildcard()
        {
            var call = await GetString("empty");

            Assert.AreEqual("data", call);
        }

        [Test]
        public async Task WithWildcard()
        {
            var call = await GetString("data/1");

            Assert.AreEqual("1", call);
        }

        [Test]
        public async Task MultipleWildcard()
        {
            var call = await GetString("data/1/time");

            Assert.AreEqual("time", call);
        }
    }
}