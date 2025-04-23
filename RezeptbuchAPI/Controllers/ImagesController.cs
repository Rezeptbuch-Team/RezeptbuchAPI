using Microsoft.AspNetCore.Mvc;
using RezeptbuchAPI.Models;

namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImagesController : ControllerBase
    {
        private readonly RecipeBookContext _context;

        public ImagesController(RecipeBookContext context)
        {
            _context = context;
        }

        [HttpGet("{hash}")]
        public IActionResult GetImageByHash(string hash)
        {
            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);

            if (recipe == null || recipe.Image == null)
            {
                return NotFound($"Image not found for recipe with hash '{hash}'. Recipe does not exist or has no image.");
            }

            return File(recipe.Image.ImageData, "image/png");
        }

        [HttpPost("{hash}")]
        public IActionResult UploadImage(string hash, [FromHeader] string uuid, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No image file provided.");
            }

            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);

            if (recipe == null)
            {
                return BadRequest($"Recipe with hash '{hash}' not found.");
            }

            if (recipe.OwnerUuid != uuid)
            {
                return BadRequest("Wrong user: You are not authorized to upload an image for this recipe.");
            }

            var image = new Image
            {
                Hash = Guid.NewGuid().ToString(),
                ContentType = imageFile.ContentType
            };

            using (var memoryStream = new MemoryStream())
            {
                imageFile.CopyTo(memoryStream);
                image.ImageData = memoryStream.ToArray();
            }


            _context.Images.Add(image);
            recipe.Image = image;

            _context.Recipes.Update(recipe);
            _context.SaveChanges();

            return Ok("Image uploaded successfully.");
        }

        [HttpPut("{hash}")]
        public IActionResult UpdateImage(string hash, [FromHeader] string uuid, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No image file provided.");
            }

            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);

            if (recipe == null)
            {
                return BadRequest($"Recipe with hash '{hash}' not found.");
            }

            if (recipe.OwnerUuid != uuid)
            {
                return BadRequest("Wrong user: You are not authorized to update the image for this recipe.");
            }

            var image = recipe.Image ?? new Image { Hash = Guid.NewGuid().ToString() };

            using (var memoryStream = new MemoryStream())
            {
                imageFile.CopyTo(memoryStream);
                image.ImageData = memoryStream.ToArray();
                image.ContentType = imageFile.ContentType;
            }

            if (recipe.Image == null)
            {
                _context.Images.Add(image);
            }
            else
            {
                _context.Images.Update(image);
            }

            _context.Recipes.Update(recipe);
            _context.SaveChanges();

            return Ok("Image updated successfully.");
        }
    }
}
