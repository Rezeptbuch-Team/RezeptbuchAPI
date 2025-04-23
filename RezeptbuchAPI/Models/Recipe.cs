namespace RezeptbuchAPI.Models
{
    public class Recipe
    {
        public string Hash { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Categories { get; set; }
        public string CookingTime { get; set; }
    }
}
