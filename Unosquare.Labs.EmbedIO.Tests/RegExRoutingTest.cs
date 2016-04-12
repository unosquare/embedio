using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    [TestFixture]
    public class RegExRoutingTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer =
                new WebServer(Resources.ServerAddress, Logger, RoutingStrategy.RegEx)
                    .WithWebApiController<TestRegexController>();
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebApi()
        {
            Assert.IsNotNull(WebServer.Module<WebApiModule>(), "WebServer has WebApiModule");

            Assert.AreEqual(WebServer.Module<WebApiModule>().ControllersCount, 1, "WebApiModule has one controller");
        }
        
        [Test]
        public void GetJsonDataWithRegexId()
        {
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regex/1");
        }

        [Test]
        public void GetJsonDatAsyncaWithRegexId()
        {
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexasync/1");
        }

        [Test]
        public void GetJsonDataWithRegexDate()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regexdate/" +
                           person.DoB.ToString("yyyy-MM-dd"));
        }

        [Test]
        public void GetJsonDataWithRegexWithTwoParams()
        {
            var person = PeopleRepository.Database.First();
            TestHelper.ValidatePerson(Resources.ServerAddress + TestRegexController.RelativePath + "regextwo/" +
                           person.MainSkill + "/" + person.Age);
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}