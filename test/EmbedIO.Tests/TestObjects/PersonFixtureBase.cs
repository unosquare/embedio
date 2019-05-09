namespace EmbedIO.Tests.TestObjects
{
    using Constants;
    using NUnit.Framework;
    using Swan.Formatters;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class PersonFixtureBase : FixtureBase
    {
        protected PersonFixtureBase(Action<IWebServer> builder, RoutingStrategy routeStrategy = RoutingStrategy.Regex, bool useTestWebServer = false)
            : base(builder, routeStrategy, useTestWebServer)
        {
        }

        protected async Task ValidatePerson(string url)
        {
            var current = PeopleRepository.Database.First();

            var jsonBody = await GetString(url);

            Assert.IsNotNull(jsonBody, "Json Body is not null");
            Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

            var item = Json.Deserialize<Person>(jsonBody);

            Assert.IsNotNull(item, "Json Object is not null");
            Assert.AreEqual(item.Name, current.Name, "Remote objects equality");
        }
    }
}
