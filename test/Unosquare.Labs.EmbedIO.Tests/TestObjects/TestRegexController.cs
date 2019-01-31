namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Constants;
    using Modules;

    public class TestRegexController : WebApiController
    {
        public const string RelativePath = "api/";

        public TestRegexController(IHttpContext context)
            : base(context)
        {
        }
        
        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "big")]
        public Task<bool> GetBigJson() => this.JsonResponseAsync(TimeZoneInfo.GetSystemTimeZones());

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "empty")]
        public Task<bool> GetEmpty() => this.JsonResponseAsync(new { Ok = true });

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex")]
        public Task<bool> GetPeople()
        {
            try
            {
                return this.JsonResponseAsync(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public Task<bool> GetPerson(int id)
        {
            try
            {
                return CheckPerson(id);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexopt/{id?}")]
        public Task<bool> GetPerson(int? id)
        {
            try
            {
                return id.HasValue ? CheckPerson(id.Value) : this.JsonResponseAsync(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexAsync/{id}")]
        public Task<bool> GetPersonAsync(int id)
        {
            try
            {
                return CheckPerson(id);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexdate/{date}")]
        public Task<bool> GetPerson(DateTime date)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p => p.DoB == date);

                if (item != null)
                {
                    return this.JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {date}");
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public Task<bool> GetPerson(string skill, int age)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p =>
                    string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

                if (item != null)
                {
                    return this.JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexthree/{skill}/{age?}")]
        public Task<bool> GetOptionalPerson(string skill, int? age = null)
        {
            try
            {
                var item = age == null
                    ? PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase))
                    : PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

                if (item != null)
                {
                    return this.JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        private Task<bool> CheckPerson(int id)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

            if (item == null) throw new KeyNotFoundException($"Key Not Found: {id}");
            return this.JsonResponseAsync(item);
        }
    }
}