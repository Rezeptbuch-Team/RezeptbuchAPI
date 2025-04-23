using Microsoft.AspNetCore.Mvc;

namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("categories")]
    public class CategoriesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCategories([FromQuery] int offset = 0, [FromQuery] int count = 10)
        {
            var categories = _context.Categories
                .OrderBy(c => c.Name)
                .Skip(offset)
                .Take(count)
                .Select(c => c.Name)
                .ToList();

            if (!categories.Any())
            {
                return BadRequest("No categories found.");
            }

            return Ok(new { categories });
        }

    }
}
