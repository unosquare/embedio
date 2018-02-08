namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Constants;
    using Modules;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    public class TestRegexController : WebApiController
    {
        public const string RelativePath = "api/";

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "empty")]
        public bool GetEmpty(WebServer server, HttpListenerContext context)
        {
            return context.JsonResponse(new {Ok = true});
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, int id)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexopt/{id?}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, int? id)
        {
            try
            {
                if (id.HasValue == false)
                {
                    return context.JsonResponse(PeopleRepository.Database);
                }

                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexAsync/{id}")]
        public async Task<bool> GetPersonAsync(WebServer server, HttpListenerContext context, int id)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return await context.JsonResponseAsync(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {id}");
            }
            catch (Exception ex)
            {
                return await context.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexdate/{date}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, DateTime date)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p => p.DoB == date);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {date}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, string skill, int age)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p =>
                    string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException($"Key Not Found: {skill}-{age}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }
    }
}