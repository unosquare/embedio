namespace Unosquare.Labs.EmbedIO.Samples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Tubular;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Tubular.ObjectModel;

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

                // otherwise, we need to parse the key and respond with the entity accordingly
                int key = 0;
                if (int.TryParse(lastSegment, out key))
                {
                    var single = _dbContext.People.Single(key);

                    if (single != null)
                        return context.JsonResponse(single);
                }

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                // here the error handler will respond with a generic 500 HTTP code a JSON-encoded object
                // with error info. You will need to handle HTTP status codes correctly depending on the situation.
                // For example, for keys that are not found, ou will need to respond with a 404 status code.
                return HandleError(context, ex, (int) HttpStatusCode.InternalServerError);
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
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "people/*")]
        public async Task<bool> PostPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                var model = context.ParseJson<GridDataRequest>();
                var data = await _dbContext.People.SelectAsync(null, null);

                return context.JsonResponse(model.CreateGridDataResponse(data.AsQueryable()));
            }
            catch (Exception ex)
            {
                // here the error handler will respond with a generic 500 HTTP code a JSON-encoded object
                // with error info. You will need to handle HTTP status codes correctly depending on the situation.
                // For example, for keys that are not found, ou will need to respond with a 404 status code.
                return HandleError(context, ex);
            }
        }

        /// <summary>
        /// Handles the error returning an error status code and json-encoded body.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns></returns>
        protected bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
        {
            var errorResponse = new
            {
                Title = "Unexpected Error",
                ErrorCode = ex.GetType().Name,
                Description = ex.ExceptionMessage(),
            };

            context.Response.StatusCode = statusCode;
            return context.JsonResponse(errorResponse);
        }
    }
}