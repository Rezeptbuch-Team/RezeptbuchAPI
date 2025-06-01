using RezeptbuchAPI.Models;
using System.Collections.Generic;

namespace RezeptbuchAPI.Models.DTO
{
    public static class RecipeImportMapper
    {
        // Punkt 1: Kategorien-Mapping entfernt!
        public static Recipe MapToRecipe(RecipeXmlImport importRecipe, string ownerUuid)
        {
            var recipe = new Recipe
            {
                Hash = string.IsNullOrWhiteSpace(importRecipe.Hash) ? System.Guid.NewGuid().ToString("N") : importRecipe.Hash,
                Title = importRecipe.Title,
                ImageName = importRecipe.ImageName,
                Description = importRecipe.Description,
                Servings = importRecipe.Servings,
                CookingTime = importRecipe.CookingTime,
                OwnerUuid = ownerUuid,
                Instructions = new List<Instruction>(),
                Categories = new List<Category>() // Wird im Controller gesetzt!
            };

            if (importRecipe.Instructions != null)
            {
                foreach (var instrDto in importRecipe.Instructions)
                {
                    var instruction = Instruction.FromXmlDto(instrDto);
                    instruction.Recipe = recipe;
                    if (instruction.Ingredients != null)
                    {
                        foreach (var ingredient in instruction.Ingredients)
                        {
                            ingredient.Instruction = instruction;
                        }
                    }
                    recipe.Instructions.Add(instruction);
                }
            }

            return recipe;
        }
    }
}