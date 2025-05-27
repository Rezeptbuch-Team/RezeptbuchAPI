using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Collections.Generic;
using RezeptbuchAPI.Models.DTO;

namespace RezeptbuchAPI.Models
{
    public class Instruction
    {
        [Key]
        public int Id { get; set; }

        public string Text { get; set; } // Gesamter Text (ohne Zutaten)
        public int RecipeId { get; set; }

        [XmlIgnore] // <-- HINZUGEFÜGT
        public Recipe Recipe { get; set; }

        public List<Ingredient> Ingredients { get; set; } = new();

        // --- Mapping: Datenbankmodell <-> Mixed-Content-DTO ---

        public static Instruction FromXmlDto(InstructionXmlDto dto)
        {
            var instruction = new Instruction();
            instruction.Text = "";
            foreach (var part in dto.Content)
            {
                if (part is string s)
                {
                    instruction.Text += s;
                }
                else if (part is Ingredient ingredient)
                {
                    instruction.Ingredients.Add(ingredient);
                    instruction.Text += $"{{ingredient_{instruction.Ingredients.Count - 1}}}"; // Platzhalter
                }
            }
            return instruction;
        }

        public InstructionXmlDto ToXmlDto()
        {
            var dto = new InstructionXmlDto();
            int ingredientIndex = 0;
            int lastPos = 0;
            string text = Text ?? "";

            // Zutaten-Platzhalter im Text suchen und Content-Liste aufbauen
            while (ingredientIndex < Ingredients.Count)
            {
                var placeholder = $"{{ingredient_{ingredientIndex}}}";
                int pos = text.IndexOf(placeholder, lastPos);
                if (pos == -1) break;

                if (pos > lastPos)
                    dto.Content.Add(text.Substring(lastPos, pos - lastPos));
                dto.Content.Add(Ingredients[ingredientIndex]);
                lastPos = pos + placeholder.Length;
                ingredientIndex++;
            }
            if (lastPos < text.Length)
                dto.Content.Add(text.Substring(lastPos));

            return dto;
        }
    }
}