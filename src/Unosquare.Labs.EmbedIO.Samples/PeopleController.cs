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

        public PeopleController(IHttpContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets the big json.
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Get, RelativePath + "big")]
        public Task<bool> GetBigJson() => this.JsonResponseAsync(Enumerable.Range(1, 20).Select(x => new
        {
            x,
            y = TimeZoneInfo.GetSystemTimeZones()
                    .Select(z => new { z.StandardName, z.DisplayName })
        }));

        /// <summary>
        /// Gets the people.
        /// This will respond to 
        ///     GET http://localhost:9696/api/people/
        ///     GET http://localhost:9696/api/people/1
        ///     GET http://localhost:9696/api/people/{n}
        /// 
        /// Notice the wildcard is important
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Get, RelativePath + "people/*")]
        public Task<bool> GetPeople()
        {
            try
            {
                // read the last segment
                var lastSegment = Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return this.JsonResponseAsync(_dbContext.People.SelectAll());

                // if it ends with "first" means we need to show first record of people
                if (lastSegment.EndsWith("first"))
                    return this.JsonResponseAsync(_dbContext.People.SelectAll().First());

                // otherwise, we need to parse the key and respond with the entity accordingly
                if (!int.TryParse(lastSegment, out var key))
                    throw new KeyNotFoundException("Key Not Found: " + lastSegment);

                var single = _dbContext.People.Single(key);

                if (single != null)
                    return this.JsonResponseAsync(single);

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        /// <summary>
        /// Posts the people Tubular model.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "people/*")]
        public async Task<bool> PostPeople()
        {
            try
            {
                var model = await this.ParseJsonAsync<GridDataRequest>();
                var data = await _dbContext.People.SelectAllAsync();

                return await this.JsonResponseAsync(model.CreateGridDataResponse(data.AsQueryable()));
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }

        /// <summary>
        /// Echoes the request form data in JSON format
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "echo/*")]
        public async Task<bool> Echo()
        {
            try
            {
                var content = await this.RequestFormDataDictionaryAsync();

                return await this.JsonResponseAsync(content);
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }
    }
}