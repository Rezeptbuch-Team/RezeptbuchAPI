namespace RezeptbuchAPI.Models.DTO
{
    public class IngredientDto
    {
        public string Name { get; set; }
        public int Amount { get; set; }
        public string Unit { get; set; }

        public static IngredientDto FromEntity(Ingredient ing) => new IngredientDto
        {
            Name = ing.Name,
            Amount = ing.Amount,
            Unit = ing.Unit
        };
    }
}