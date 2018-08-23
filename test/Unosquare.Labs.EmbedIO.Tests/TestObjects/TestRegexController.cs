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
        public bool GetEmpty()
        {
            return this.JsonResponse(new {Ok = true});
        }

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
                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return this.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
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
                if (id.HasValue == false)
                {
                    return this.JsonResponse(PeopleRepository.Database);
                }

                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return this.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
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
                await Task.Delay(TimeSpan.FromSeconds(1));

                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return await this.JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
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
    }
}