using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MoviesAPI.Entities;
using MoviesAPI.TestEntities;
using System.Runtime.CompilerServices;

namespace MoviesAPI.Controllers
{
 
        [Route("api/[controller]")]
        [ApiController]
        public class GenresController: ControllerBase
        {
        private readonly IRepository _repository;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IConfiguration configuration;
        private const string cacheTag = "genres";

        public GenresController(IRepository repository,
            IOutputCacheStore outputCacheStore,
            IConfiguration configuration
            )
        {
            _repository = repository;
            this.outputCacheStore = outputCacheStore;
            this.configuration = configuration;
        }

        [HttpGet] //api/genre
        [HttpGet("list")]
        [HttpGet("list-genre")]
        [OutputCache(Tags = [cacheTag])]
        public List<Genre> Get()
            {
               
                var genres = _repository.ObtainAllGenres();
                return genres;
            }

        [HttpGet("{id:int}")]
        [OutputCache(Tags = [cacheTag])]
        public async Task<ActionResult<Genre>> GetById(int id)
        {
        
            var genre = await _repository.ObtainGenreById(id);
            if (genre is null)
            {
                return NotFound();
            }
            return genre;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] Genre genre)
        {

    
            var genreExistsAlready = _repository.Exists(genre.Name);
            if (genreExistsAlready)
            {
                return BadRequest($"The genre with the name {genre.Name} exists already");
            }
            _repository.AddGenre(genre);
            await outputCacheStore.EvictByTagAsync(cacheTag, default);
            return Ok();
        }
   
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] Genre genre)
        {
            if (id != genre.Id)
                return BadRequest("ID mismatch");


            try
            {
      
                await _repository.UpdateGenre(genre);
                await outputCacheStore.EvictByTagAsync(cacheTag, default); // 💥 Invalida la caché
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
      
            try
            {
                await _repository.DeleteGenre(id);
                await outputCacheStore.EvictByTagAsync(cacheTag, default); // 💥 Invalida la caché
                return new OkResult();
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
        }
    }
}

