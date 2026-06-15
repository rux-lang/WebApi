using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Repositories;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkflowsController(WorkflowRepository repository) : ControllerBase
    {
        private readonly WorkflowRepository repository = repository;

        [HttpGet]
        public async Task<ActionResult<List<Workflow>>> GetAll()
        {
            return await repository.GetAllAsync();
        }
    }
}
