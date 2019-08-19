using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Swan.Formatters;

namespace EmbedIO.Tests.TestObjects
{
    public abstract class PersonEndToEndFixtureBase : EndToEndFixtureBase
    {
        protected PersonEndToEndFixtureBase(bool useTestWebServer)
            : base(useTestWebServer)
        {
        }

        protected async Task ValidatePersonAsync(string url)
        {
            var current = PeopleRepository.Database.First();

            var jsonBody = await Client.GetStringAsync(url);

            Assert.IsNotNull(jsonBody, "Json Body is not null");
            Assert.IsNotEmpty(jsonBody, "Json Body is not empty");

            var item = Json.Deserialize<Person>(jsonBody);

            Assert.IsNotNull(item, "Json Object is not null");
            Assert.AreEqual(item.Name, current.Name, "Remote objects equality");
        }
    }
}