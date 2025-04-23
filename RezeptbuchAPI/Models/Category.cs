using System.ComponentModel.DataAnnotations;

namespace RezeptbuchAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
