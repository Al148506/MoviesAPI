using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MoviesAPI.Entities;
using MoviesAPI.TestEntities;

namespace MoviesAPI.Controllers
{ 
    [Route("api/[controller]")]
    [ApiController]
    public class GenreSQLController : ControllerBase
    {
       
        private readonly IOutputCacheStore outputCacheStore;
        private const string cacheTag = "genres";

        public GenreSQLController(IRepository repository,
            IOutputCacheStore outputCacheStore

            )
        {
        
            this.outputCacheStore = outputCacheStore;
    
        }

        [HttpGet] //api/genre
        [OutputCache(Tags = [cacheTag])]
        public List<Genre> Get()
        {
            return new List<Genre>() { new Genre { Id = 1, Name = "Comedy" }, new Genre { Id = 2, Name = "Action" } };
        }

        [HttpGet("{id:int}")]
        [OutputCache(Tags = [cacheTag])]
        public async Task<ActionResult<Genre>> GetById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] Genre genre)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] Genre genre)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {

            throw new NotImplementedException();
        }
    }
}

