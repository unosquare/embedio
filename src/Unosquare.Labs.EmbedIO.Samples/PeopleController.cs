namespace Unosquare.Labs.EmbedIO.Samples
{
    using Constants;
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
        /// Gets the people.
        /// This will respond to 
        ///     GET http://localhost:9696/api/people/
        ///     GET http://localhost:9696/api/people/1
        ///     GET http://localhost:9696/api/people/{n}
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Get, RelativePath + "people/{id?}")]
        public async Task<bool> GetPeople(string id = null)
        {
            // if it ends with a / means we need to list people
            if (string.IsNullOrWhiteSpace(id))
                return await this.JsonResponseAsync(_dbContext.People.SelectAll());

            // if it ends with "first" means we need to show first record of people
            if (id == "first")
                return await this.JsonResponseAsync(_dbContext.People.SelectAll().First());

            // otherwise, we need to parse the key and respond with the entity accordingly
            if (int.TryParse(id, out var key))
            {
                var single = await _dbContext.People.SingleAsync(key);

                if (single != null)
                    return await this.JsonResponseAsync(single);
            }

            throw new KeyNotFoundException($"Key Not Found: {id}");
        }

        /// <summary>
        /// Posts the people Tubular model.
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "people/")]
        public Task<bool> PostPeople() =>
            this.TransformJson<GridDataRequest, GridDataResponse>(async (model, ct) =>
                model.CreateGridDataResponse((await _dbContext.People.SelectAllAsync()).AsQueryable()));

        /// <summary>
        /// Echoes the request form data in JSON format
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "echo/")]
        public async Task<bool> Echo()
        {
            var content = await this.RequestFormDataDictionaryAsync();

            return await this.JsonResponseAsync(content);
        }
    }
}