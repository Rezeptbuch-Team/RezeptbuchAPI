using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RezeptbuchAPI.Models
{
    public class Image
    {
        public string Hash { get; set; }
        public byte[] ImageData { get; set; }
        public string ContentType { get; set; }
    }

}
