using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Unosquare.Tubular;
using Unosquare.Tubular.ObjectModel;

namespace EmbedIO.Samples
{
    // A very simple controller to handle People CRUD.
    // Notice how it Inherits from WebApiController and the methods have WebApiHandler attributes 
    // This is for sampling purposes only.
    public sealed class PeopleController : WebApiController, IDisposable
    {
        private readonly AppDbContext _dbContext = new AppDbContext();

        public PeopleController(IHttpContext context, CancellationToken cancellationToken)
            : base(context, cancellationToken)
        {
        }

        public void Dispose() => _dbContext.Dispose();

        // Gets all records.
        // This will respond to 
        //     GET http://localhost:9696/api/people
        [RouteHandler(HttpVerbs.Get, "/people")]
        public Task<bool> GetAllPeople() => Ok(_dbContext.People.SelectAll());

        // Gets the first record.
        // This will respond to 
        //     GET http://localhost:9696/api/people/first
        [RouteHandler(HttpVerbs.Get, "/people/first")]
        public Task<bool> GetFirstPeople()
            => Ok(_dbContext.People.SelectAll().First());

        // Gets a single record.
        // This will respond to 
        //     GET http://localhost:9696/api/people/1
        //     GET http://localhost:9696/api/people/{n}
        //
        // If the given ID is not found, this method will return false.
        // By default, WebApiModule will then respond with "404 Not Found".
        //
        // If the given ID cannot be converted to an integer, an exception will be thrown.
        // By default, WebApiModule will then respond with "500 Internal Server Error".
        [RouteHandler(HttpVerbs.Get, "/people/{id}")]
        public async Task<bool> GetPeople(int id)
        {
            var single = await _dbContext.People.SingleAsync(id).ConfigureAwait(false);
            return single != null && await Ok(single).ConfigureAwait(false);
        }

        // Posts the people Tubular model.
        [RouteHandler(HttpVerbs.Post, "/people")]
        public Task<bool> PostPeople() =>
            Ok<GridDataRequest, GridDataResponse>(async (model, ct) =>
                model.CreateGridDataResponse((await _dbContext.People.SelectAllAsync().ConfigureAwait(false)).AsQueryable()));

        // Echoes request form data in JSON format.
        [RouteHandler(HttpVerbs.Post, "/echo")]
        public async Task<bool> Echo()
        {
            var content = await HttpContext.GetRequestFormDataAsync(CancellationToken).ConfigureAwait(false);
            return await Ok(content).ConfigureAwait(false);
        }
    }
}