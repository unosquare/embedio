﻿namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
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
        public Task<bool> GetBigJson() => JsonResponseAsync(Enumerable.Range(1, 100).Select(x => new
        {
            x,
            y = TimeZoneInfo.GetSystemTimeZones()
                .Select(z => new { z.StandardName, z.DisplayName }),
        }));

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "empty")]
        public Task<bool> GetEmpty() => JsonResponseAsync(new { Ok = true });

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex")]
        public Task<bool> GetPeople()
        {
            try
            {
                return JsonResponseAsync(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return JsonExceptionResponseAsync(ex);
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
                return JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexopt/{id?}")]
        public Task<bool> GetPerson(int? id)
        {
            try
            {
                return id.HasValue ? CheckPerson(id.Value) : JsonResponseAsync(PeopleRepository.Database);
            }
            catch (Exception ex)
            {
                return JsonExceptionResponseAsync(ex);
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
                return JsonExceptionResponseAsync(ex);
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
                    return JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {date}");
            }
            catch (Exception ex)
            {
                return JsonExceptionResponseAsync(ex);
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
                    return JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return JsonExceptionResponseAsync(ex);
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
                    return JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return JsonExceptionResponseAsync(ex);
            }
        }

        private Task<bool> CheckPerson(int id)
        {
            var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

            if (item == null) throw new KeyNotFoundException($"Key Not Found: {id}");
            return JsonResponseAsync(item);
        }
    }
}