using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Modules;
using Unosquare.Tubular;
using Unosquare.Tubular.ObjectModel;

namespace EmbedIO.Samples
{
    /// <summary>
    /// A very simple controller to handle People CRUD.
    /// Notice how it Inherits from WebApiController and the methods have WebApiHandler attributes 
    /// This is for sampling purposes only.
    /// </summary>
    public sealed class PeopleController : WebApiController, IDisposable
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
                return await Ok(_dbContext.People.SelectAll()).ConfigureAwait(false);

            // if it ends with "first" means we need to show first record of people
            if (id == "first")
                return await Ok(_dbContext.People.SelectAll().First()).ConfigureAwait(false);

            // otherwise, we need to parse the key and respond with the entity accordingly
            if (int.TryParse(id, out var key))
            {
                var single = await _dbContext.People.SingleAsync(key).ConfigureAwait(false);

                if (single != null)
                    return await Ok(single).ConfigureAwait(false);
            }

            throw new KeyNotFoundException($"Key Not Found: {id}");
        }

        /// <summary>
        /// Posts the people Tubular model.
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "people/")]
        public Task<bool> PostPeople() =>
            Ok<GridDataRequest, GridDataResponse>(async (model, ct) =>
                model.CreateGridDataResponse((await _dbContext.People.SelectAllAsync().ConfigureAwait(false)).AsQueryable()));

        /// <summary>
        /// Echoes the request form data in JSON format
        /// </summary>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RelativePath + "echo/")]
        public async Task<bool> Echo()
        {
            var content = await HttpContext.RequestFormDataDictionaryAsync().ConfigureAwait(false);

            return await Ok(content).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose() => _dbContext?.Dispose();
    }
}