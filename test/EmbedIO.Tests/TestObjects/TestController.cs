using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Modules;

namespace EmbedIO.Tests.TestObjects
{
    public class TestController : WebApiController
    {
        public const string RelativePath = "api/";
        public const string EchoPath = RelativePath + "echo/";
        public const string GetPath = RelativePath + "people/";

        public TestController(IHttpContext context)
            : base(context)
        {
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "big")]
        public Task<bool> GetBigJson() => Ok(Enumerable.Range(1, 100).Select(x => new
        {
            x,
            y = TimeZoneInfo.GetSystemTimeZones()
                .Select(z => new { z.StandardName, z.DisplayName }),
        }));

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "empty")]
        public Task<bool> GetEmpty() => Ok(new { Ok = true });

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex")]
        public Task<bool> GetPeople() => Ok(PeopleRepository.Database);
        
        [WebApiHandler(HttpVerbs.Get, "/" + GetPath)]
        public Task<bool> GetAllPeople() => Ok(PeopleRepository.Database);

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public Task<bool> GetPerson(int id) => CheckPerson(id);

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexopt/{id?}")]
        public Task<bool> GetPerson(int? id) => id.HasValue ? CheckPerson(id.Value) : Ok(PeopleRepository.Database);

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexAsync/{id}")]
        public Task<bool> GetPersonAsync(int id) => CheckPerson(id);

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexdate/{date}")]
        public Task<bool> GetPerson(DateTime date)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p => p.DoB == date);

            return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {date}");
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public Task<bool> GetPerson(string skill, int age)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p =>
                string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

            return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexthree/{skill}/{age?}")]
        public Task<bool> GetOptionalPerson(string skill, int? age = null)
        {
            var item = age == null
                ? PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase))
                : PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

            return item != null ? Ok(item) : throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
        }

        [WebApiHandler(HttpVerbs.Post, "/" + EchoPath)]
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