using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RezeptbuchAPI.Models;
using RezeptbuchAPI.Models.DTO;

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

        // GET /images/{hash}
        [HttpGet("{hash}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetImageByHash(string hash)
        {
            // Image explizit mitladen!
            var recipe = _context.Recipes
                .Include(r => r.Image)
                .FirstOrDefault(r => r.Hash == hash);

            if (recipe == null || recipe.Image == null)
                return NotFound("Image not found (recipe does not exist or has no image).");

            var contentType = recipe.Image.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "application/octet-stream";

            return File(recipe.Image.ImageData, contentType);
        }

        // POST /images/{hash}
        [HttpPost("{hash}")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadImage(
            string hash,
            [FromHeader(Name = "uuid")] string uuid,
            [FromForm] ImageUploadDto dto)
        {
            var imageFile = dto.ImageFile;
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file provided.");


            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);
            if (recipe == null)
                return BadRequest("Recipe with given hash not found.");

            if (recipe.OwnerUuid != uuid)
                return BadRequest("Wrong user: You are not authorized to upload an image for this recipe.");

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

        // PUT /images/{hash}
        [HttpPut("{hash}")]
        [Consumes("multipart/form-data")]
        public IActionResult UpdateImage(
            string hash,
            [FromHeader(Name = "uuid")] string uuid,
            [FromForm] ImageUploadDto dto)
        {
            var imageFile = dto.ImageFile;
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file provided.");

            var recipe = _context.Recipes.FirstOrDefault(r => r.Hash == hash);
            if (recipe == null)
                return BadRequest("Recipe with given hash not found.");

            if (recipe.OwnerUuid != uuid)
                return BadRequest("Wrong user: You are not authorized to update the image for this recipe.");

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
                recipe.Image = image;
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