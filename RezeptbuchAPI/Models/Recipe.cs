using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RezeptbuchAPI.Models
{
    public class Recipe
    {
        public string Hash { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CookingTime { get; set; }
        public string OwnerUuid { get; set; }

        public Image Image { get; set; }

        public List<string> Categories { get; set; } = new();

    }

}