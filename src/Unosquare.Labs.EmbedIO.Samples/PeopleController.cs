namespace Unosquare.Labs.EmbedIO.Samples
{
    using Constants;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Modules;
    using System.Threading.Tasks;
    using Tubular;
    using Tubular.ObjectModel;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <inheritdoc />
    /// <summary>
    /// A very simple controller to handle People CRUD.
    /// Notice how it Inherits from WebApiController and the methods have WebApiHandler attributes 
    /// This is for sampling purposes only.
    /// </summary>
    public class PeopleController : WebApiController
    {
        private readonly AppDbContext _dbContext = new AppDbContext();
        private const string RelativePath = "/api/";

        /// <summary>
        /// Gets the people.
        /// This will respond to 
        ///     GET http://localhost:9696/api/people/
        ///     GET http://localhost:9696/api/people/1
        ///     GET http://localhost:9696/api/people/{n}
        /// 
        /// Notice the wildcard is important
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Get, RelativePath + "people/*")]
        public bool GetPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponse(_dbContext.People.SelectAll());

                // if it ends with "first" means we need to show first record of people
                if (lastSegment.EndsWith("first"))
                    return context.JsonResponse(_dbContext.People.SelectAll().First());

                // otherwise, we need to parse the key and respond with the entity accordingly
                if (!int.TryParse(lastSegment, out var key))
                    throw new KeyNotFoundException("Key Not Found: " + lastSegment);

                var single = _dbContext.People.Single(key);

                if (single != null)
                    return context.JsonResponse(single);

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        /// <summary>
        /// Posts the people Tubular model.
        /// This will respond to 
        ///     GET http://localhost:9696/api/people/
        ///     GET http://localhost:9696/api/people/1
        ///     GET http://localhost:9696/api/people/{n}
        /// 
        /// Notice the wildcard is important
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "people/*")]
        public async Task<bool> PostPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                var model = context.ParseJson<GridDataRequest>();
                var data = await _dbContext.People.SelectAllAsync();

                return context.JsonResponse(model.CreateGridDataResponse(data.AsQueryable()));
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        /// <summary>
        /// Echoes the request form data in JSON format
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "echo/*")]
        public bool Echo(WebServer server, HttpListenerContext context)
        {
            try
            {
                var content = context.RequestFormDataDictionary();

                return context.JsonResponse(content);
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }
    }
}