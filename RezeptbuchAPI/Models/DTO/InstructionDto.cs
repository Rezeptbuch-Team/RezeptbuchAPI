using System.Collections.Generic;
using System.Linq;

namespace RezeptbuchAPI.Models.DTO
{
    public class InstructionDto
    {
        public string Text { get; set; }
        public List<IngredientDto> Ingredients { get; set; }

        public static InstructionDto FromEntity(Instruction instr) => new InstructionDto
        {
            Text = instr.Text,
            Ingredients = instr.Ingredients?.Select(IngredientDto.FromEntity).ToList() ?? new()
        };
    }
}