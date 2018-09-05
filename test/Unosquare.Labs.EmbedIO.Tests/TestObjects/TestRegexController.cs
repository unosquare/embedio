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

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "empty")]
        public bool GetEmpty() => this.JsonResponse(new {Ok = true});

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex")]
        public bool GetPeople()
        {
            try
            {
                return this.JsonResponse(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public bool GetPerson(int id)
        {
            try
            {
                return CheckPerson(id);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexopt/{id?}")]
        public bool GetPerson(int? id)
        {
            try
            {
                return id.HasValue ? CheckPerson(id.Value) : this.JsonResponse(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexAsync/{id}")]
        public async Task<bool> GetPersonAsync(int id)
        {
            try
            {
                return CheckPerson(id);
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexdate/{date}")]
        public bool GetPerson(DateTime date)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p => p.DoB == date);

                if (item != null)
                {
                    return this.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {date}");
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public bool GetPerson(string skill, int age)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p =>
                    string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

                if (item != null)
                {
                    return this.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }
        
        private bool CheckPerson(int id)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

            if (item == null) throw new KeyNotFoundException($"Key Not Found: {id}");
            return this.JsonResponse(item);
        }
    }
}