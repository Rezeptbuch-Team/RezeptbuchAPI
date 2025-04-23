using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RezeptbuchAPI.Models
{
    public class Image
    {
        public string Hash { get; set; }
        public byte[] ImageData { get; set; } // This should hold the image binary data
        public string ContentType { get; set; } // To store the image content type (e.g., image/jpeg)
    }

}
