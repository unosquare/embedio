using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public class TestController : WebApiController
    {
        public const string EchoPath = "echo";

        public TestController(IHttpContext context, CancellationToken cancellationToken)
            : base(context, cancellationToken)
        {
        }

        [Route(HttpVerbs.Get, "/empty")]
        public object GetEmpty() => new { Ok = true };

        [Route(HttpVerbs.Get, "/regex")]
        public object GetPeople() => PeopleRepository.Database;

        [Route(HttpVerbs.Post, "/regex")]
        public async Task<object> PostPeople()
            => await HttpContext.GetRequestDataAsync(RequestDeserializer.Json<Person>, CancellationToken)
                .ConfigureAwait(false);

        [Route(HttpVerbs.Get, "/regex/{id}")]
        public object GetPerson(int id) => CheckPerson(id);

        [Route(HttpVerbs.Get, "/regexopt/{id?}")]
        public object GetPerson(int? id)
            => id.HasValue ? CheckPerson(id.Value) : PeopleRepository.Database;

        [Route(HttpVerbs.Get, "/regexdate/{date}")]
        public object GetPerson(DateTime date)
            => PeopleRepository.Database.FirstOrDefault(p => p.DoB == date)
            ?? throw HttpException.NotFound();

        [Route(HttpVerbs.Get, "/regextwo/{skill}/{age}")]
        public object GetPerson(string skill, int age)
            => PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age)
            ?? throw HttpException.NotFound();

        [Route(HttpVerbs.Get, "/regexthree/{skill}/{age?}")]
        public object GetOptionalPerson(string skill, int? age = null)
        {
            var item = age == null
                ? PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase))
                : PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

            return item ?? throw HttpException.NotFound();
        }

        [Route(HttpVerbs.Post, "/" + EchoPath)]
        public async Task<object> PostEcho()
            => await HttpContext.GetRequestFormDataAsync(CancellationToken).ConfigureAwait(false);

        private object CheckPerson(int id)
            =>PeopleRepository.Database.FirstOrDefault(p => p.Key == id)
            ?? throw HttpException.NotFound();
    }
}