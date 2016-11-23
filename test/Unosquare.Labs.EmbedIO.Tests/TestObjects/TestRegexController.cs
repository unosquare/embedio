using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
#if NET46
    using System.Net;
#else
using Unosquare.Net;
#endif

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class TestRegexController : WebApiController
    {
        public const string RelativePath = "api/";
        public int errorCode = (int)System.Net.HttpStatusCode.InternalServerError;

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, int id)
        {
            try
            {
                if (PeopleRepository.Database.Any(p => p.Key == id))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == id));
                }

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = errorCode;
                return context.JsonResponse(ex);
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

                if (PeopleRepository.Database.Any(p => p.Key == id))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == id));
                }

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = errorCode;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexAsync/{id}")]
        public async Task<bool> GetPersonAsync(WebServer server, HttpListenerContext context, int id)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (PeopleRepository.Database.Any(p => p.Key == id))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == id));
                }

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = errorCode;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regexdate/{date}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, DateTime date)
        {
            try
            {
                if (PeopleRepository.Database.Any(p => p.DoB == date))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.DoB == date));
                }

                throw new KeyNotFoundException("Key Not Found: " + date);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = errorCode;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, string skill, int age)
        {
            try
            {
                if (PeopleRepository.Database.Any(p => p.MainSkill.ToLower() == skill.ToLower() && p.Age == age))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.MainSkill.ToLower() == skill.ToLower() && p.Age == age));
                }

                throw new KeyNotFoundException("Key Not Found: " + skill + "-" + age);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = errorCode;
                return context.JsonResponse(ex);
            }
        }
    }
}