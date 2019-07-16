using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Utilities;
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

        public void Dispose() => _dbContext.Dispose();

        // Gets all records.
        // This will respond to 
        //     GET http://localhost:9696/api/people
        [Route(HttpVerbs.Get, "/people")]
        public async Task<object> GetAllPeople() => await _dbContext.People.SelectAllAsync().ConfigureAwait(false);

        // Gets the first record.
        // This will respond to 
        //     GET http://localhost:9696/api/people/first
        [Route(HttpVerbs.Get, "/people/first")]
        public async Task<object> GetFirstPeople() => (await _dbContext.People.SelectAllAsync().ConfigureAwait(false)).First();

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
        [Route(HttpVerbs.Get, "/people/{id?}")]
        public async Task<object> GetPeople(int id)
            => await _dbContext.People.SingleAsync(id).ConfigureAwait(false)
            ?? throw HttpException.NotFound();

        // Posts the people Tubular model.
        [Route(HttpVerbs.Post, "/people")]
        public async Task<object> PostPeople([JsonGridDataRequest] GridDataRequest gridDataRequest)
            => gridDataRequest.CreateGridDataResponse((await _dbContext.People.SelectAllAsync().ConfigureAwait(false)).AsQueryable());

        // Echoes request form data in JSON format.
        [Route(HttpVerbs.Post, "/echo")]
        public object Echo([FormData] NameValueCollection data)
            => data.ToDictionary();

        // Select by name
        [Route(HttpVerbs.Get, "/peopleByName/{name}")]
        public async Task<object> GetPeopleByName(string name)
            => await _dbContext.People.FirstOrDefaultAsync(nameof(Person.Name), name).ConfigureAwait(false)
            ?? throw HttpException.NotFound();

        // Session handler
        [Route(HttpVerbs.Get, "/session")]
        public object GetSession()
        {
            var isEmpty = HttpContext.Session.IsEmpty;

            HttpContext.Session["Id"] = HttpContext.Session.Count == 0 ? Guid.Empty : Guid.NewGuid();

            return new
            {
                isEmpty,
                Guid = HttpContext.Session["Id"],
                HttpContext.Session.Id,
            };
        }
    }
}