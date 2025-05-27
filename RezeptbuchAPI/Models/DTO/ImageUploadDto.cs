using Microsoft.AspNetCore.Http;

namespace RezeptbuchAPI.Models.DTO
{
    public class ImageUploadDto
    {
        public IFormFile ImageFile { get; set; }
    }
}