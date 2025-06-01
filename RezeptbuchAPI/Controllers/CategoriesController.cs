using Microsoft.AspNetCore.Mvc;
using RezeptbuchAPI.Models;


namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RecipeBookContext _context;

        // Inject RecipeBookContext into the controller via constructor
        public CategoriesController(RecipeBookContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetCategories([FromQuery] int offset = 0, [FromQuery] int? count = null)
        {
            if (count == null)
                return BadRequest("Parameter “count” is required.");
            if (count <= 0)
                return BadRequest("Parameter “count” must be greater than 0.");
            if (count > 50)
                return BadRequest("Parameter “count” may be a maximum of 50.");

            var categories = _context.Categories
                .OrderBy(c => c.Name)
                .Skip(offset)
                .Take(count.Value)
                .Select(c => c.Name)
                .ToList();

            if (!categories.Any())
                 return BadRequest("No categories found.");

            return Ok(new { categories });
        }

    }
}