using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests.TestObjects
{
    public abstract class PersonFixtureBase : FixtureBase
    {
        protected PersonFixtureBase(Action<IWebServer> builder, bool useTestWebServer = false)
            : base(builder, useTestWebServer)
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