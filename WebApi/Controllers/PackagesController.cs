using Microsoft.AspNetCore.Mvc;
using Npgsql;
using WebApi.Models;
using WebApi.Repositories;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PackagesController(PackageRepository repository) : ControllerBase
    {
        private readonly PackageRepository repository = repository;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Package>>> GetAll()
        {
            return await repository.GetAllAsync();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Package>> GetById(Guid id)
        {
            var package = await repository.GetByIdAsync(id);
            if (package is null)
            {
                return NotFound();
            }
            return package;
        }

        [HttpPost]
        public async Task<ActionResult<Package>> Create(PackageRequest request)
        {
            var package = new Package
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                Description = request.Description,
                Repository = request.Repository,
                License = request.License,
                Created = DateTime.UtcNow
            };
            try
            {
                await repository.CreateAsync(package);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict("A package with the same name or repository already exists.");
            }
            return CreatedAtAction(nameof(GetById), new { id = package.Id }, package);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, PackageRequest request)
        {
            var package = new Package
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                Repository = request.Repository,
                License = request.License
            };
            bool updated;
            try
            {
                updated = await repository.UpdateAsync(package);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict("A package with the same name or repository already exists.");
            }

            if (!updated)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await repository.DeleteAsync(id))
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
