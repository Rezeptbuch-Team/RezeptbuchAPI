using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RezeptbuchAPI.Models.DTO
{
    public class RecipeDto
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; }

        [JsonPropertyName("cooking_time")]
        public int CookingTime { get; set; }

        public string ImageName { get; set; }
        public int? Servings { get; set; }

        // WICHTIG: Jetzt List<InstructionDto>
        public List<InstructionDto> Instructions { get; set; }
    }
}