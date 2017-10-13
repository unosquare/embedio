using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
#if NET47
    using System.Net;
#else
using Unosquare.Net;
#endif

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class TestRegexController : WebApiController
    {
        public const string RelativePath = "api/";
        public int ErrorCode = (int) System.Net.HttpStatusCode.InternalServerError;

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

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = ErrorCode;
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

                var item = PeopleRepository.Database.FirstOrDefault(p => p.Key == id);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = ErrorCode;
                return context.JsonResponse(ex);
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

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = ErrorCode;
                return await context.JsonResponseAsync(ex);
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

                throw new KeyNotFoundException("Key Not Found: " + date);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = ErrorCode;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regextwo/{skill}/{age}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, string skill, int age)
        {
            try
            {
                var item = PeopleRepository.Database.FirstOrDefault(p =>
                    p.MainSkill.ToLower() == skill.ToLower() && p.Age == age);

                if (item != null)
                {
                    return context.JsonResponse(item);
                }

                throw new KeyNotFoundException("Key Not Found: " + skill + "-" + age);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = ErrorCode;
                return context.JsonResponse(ex);
            }
        }
    }
}