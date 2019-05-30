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

        [RouteHandler(HttpVerbs.Get, "/empty")]
        public Task<bool> GetEmpty() => Ok(new { Ok = true });

        [RouteHandler(HttpVerbs.Get, "/regex")]
        public Task<bool> GetPeople() => Ok(PeopleRepository.Database);
        
        [RouteHandler(HttpVerbs.Get, "/regex/{id}")]
        public Task<bool> GetPerson(int id) => CheckPerson(id);

        //[RouteHandler(HttpVerbs.Get, "/regexopt/{id?}")]
        //public Task<bool> GetPerson(int? id) => id.HasValue ? CheckPerson(id.Value) : Ok(PeopleRepository.Database);

        //[RouteHandler(HttpVerbs.Get, "/regexdate/{date}")]
        //public Task<bool> GetPerson(DateTime date)
        //{
        //    var item = PeopleRepository.Database.FirstOrDefault(p => p.DoB == date);

        //    return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {date}");
        //}

        //[RouteHandler(HttpVerbs.Get, "/regextwo/{skill}/{age}")]
        //public Task<bool> GetPerson(string skill, int age)
        //{
        //    var item = PeopleRepository.Database.FirstOrDefault(p =>
        //        string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

        //    return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
        //}

        //[RouteHandler(HttpVerbs.Get, "/regexthree/{skill}/{age?}")]
        //public Task<bool> GetOptionalPerson(string skill, int? age = null)
        //{
        //    var item = age == null
        //        ? PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase))
        //        : PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

        //    return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
        //}

        [RouteHandler(HttpVerbs.Post, "/" + EchoPath)]
        public async Task<bool> PostEcho()
        {
            var content = await HttpContext.RequestFormDataDictionaryAsync();

            return await Ok(content);
        }

        private Task<bool> CheckPerson(int id)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

            return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {id}");
        }
    }
}