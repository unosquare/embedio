﻿using NUnit.Framework;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class RegexRoutingTest : FixtureBase
    {
        public RegexRoutingTest()
            : base(ws => ws.RegisterModule(new TestRoutingModule()), Constants.RoutingStrategy.Regex)
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
                var call = await GetString($"{WebServerUrl}data/1/asdasda/dasdasasda");

                Assert.AreEqual("dasdasasda", call);
            }
        }
    }
}