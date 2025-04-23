using Microsoft.AspNetCore.Mvc;
using RezeptbuchAPI.Models;

namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("recipes")]
    public class RecipesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetRecipes(
            [FromQuery] int offset = 0,
            [FromQuery] int count = 10,
            [FromQuery] string order_by = "title",
            [FromQuery] string order = "asc",
            [FromQuery] List<string>? categories = null)
        {
            if (order_by != "title" && order_by != "cooking_time")
            {
                return BadRequest("Invalid value for 'order_by'. Must be 'title' or 'cooking_time'.");
            }

            var query = _context.Recipes.AsQueryable();

            if (categories != null && categories.Count > 0)
            {
                query = query.Where(r => r.Categories.Any(c => categories.Contains(c)));
            }

            query = order_by switch
            {
                "title" => order == "desc" ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
                "cooking_time" => order == "desc" ? query.OrderByDescending(r => r.CookingTime) : query.OrderBy(r => r.CookingTime),
                _ => query
            };

            var recipes = query
                .Skip(offset)
                .Take(Math.Min(count, 25))
                .ToList();

            return Ok(new { recipes });
        }


        [HttpPost]
        public IActionResult UploadRecipe(
            [FromHeader] string uuid,
            [FromBody] string xmlContent)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                return BadRequest("No XML content provided.");
            }

            Recipe? recipe = null;

            try
            {
                recipe = DeserializeXmlToRecipe(xmlContent);

                if (recipe == null)
                {
                    return BadRequest("Malformed XML: Unable to parse the recipe.");
                }

                if (string.IsNullOrWhiteSpace(recipe.Title) || recipe.CookingTime <= 0)
                {
                    return BadRequest("Validation error: Title or cooking time is missing or invalid.");
                }

                _context.Recipes.Add(recipe);
                _context.SaveChanges();

                return Ok("Recipe uploaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing XML: {ex.Message}");
            }
        }

        private Recipe? DeserializeXmlToRecipe(string xmlContent)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Recipe));
                using (var reader = new StringReader(xmlContent))
                {
                    return (Recipe?)serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        [HttpGet("{hash}")]
        public IActionResult GetRecipeByHash(string hash)
        {
            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);

            if (recipe == null)
            {
                return NotFound($"Recipe with hash '{hash}' not found.");
            }

            return Ok(recipe);
        }


        [HttpPut("{hash}")]
        public IActionResult UpdateRecipe(string hash, [FromHeader] string uuid, [FromBody] string xmlContent)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                return BadRequest("No XML content provided.");
            }

            Recipe? updatedRecipe = null;

            try
            {
                updatedRecipe = DeserializeXmlToRecipe(xmlContent);

                if (updatedRecipe == null)
                {
                    return BadRequest("Malformed XML: Unable to parse the recipe.");
                }

                if (string.IsNullOrWhiteSpace(updatedRecipe.Title) || updatedRecipe.CookingTime <= 0)
                {
                    return BadRequest("Validation error: Title or cooking time is missing or invalid.");
                }

                var existingRecipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);

                if (existingRecipe == null)
                {
                    return BadRequest($"No recipe found with the specified hash '{hash}'.");
                }

                existingRecipe.Title = updatedRecipe.Title;
                existingRecipe.Description = updatedRecipe.Description;
                existingRecipe.CookingTime = updatedRecipe.CookingTime;
                existingRecipe.Categories = updatedRecipe.Categories;

                _context.Recipes.Update(existingRecipe);
                _context.SaveChanges();

                return Ok("Recipe updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing XML: {ex.Message}");
            }
        }

        private Recipe? DeserializeXmlToRecipe(string xmlContent)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Recipe));
                using (var reader = new StringReader(xmlContent))
                {
                    return (Recipe?)serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
