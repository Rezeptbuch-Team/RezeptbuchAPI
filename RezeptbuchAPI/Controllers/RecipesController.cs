using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RezeptbuchAPI.Models;
using RezeptbuchAPI.Models.DTO;
using AutoMapper;

namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("recipes")]
    public class RecipesController : ControllerBase
    {
        private readonly RecipeBookContext _context;
        private readonly IMapper _mapper;

        public RecipesController(RecipeBookContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // --- Hilfsmethoden für Kategorie-Handling und Validierung ---

        private void EnsureCategoriesExist(List<string> categories)
        {
            var trimmed = categories.Select(c => c.Trim()).ToList();
            var existingCategories = _context.Categories
                .Select(c => c.Name)
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            var newCategories = trimmed
                .Where(cat => !existingCategories.Contains(cat))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var cat in newCategories)
            {
                _context.Categories.Add(new Category { Name = cat });
            }
            if (newCategories.Count > 0)
                _context.SaveChanges();
        }

        private List<Category> GetCategoriesFromDb(List<string> categories)
        {
            var importCategorySet = categories
                .Select(c => c.Trim())
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            return _context.Categories
                .Where(c => importCategorySet.Contains(c.Name))
                .ToList();
        }

        private IActionResult ValidateCount(int? count)
        {
            if (count == null)
                return BadRequest("Parameter 'count' ist erforderlich.");
            if (count <= 0)
                return BadRequest("Parameter 'count' muss größer als 0 sein.");
            if (count > 25)
                return BadRequest("Parameter 'count' darf maximal 25 sein.");
            return null!;
        }

        private IActionResult ValidateOrderBy(string order_by)
        {
            if (order_by != "title" && order_by != "cooking_time")
                return BadRequest("Invalid value for 'order_by'. Must be 'title' or 'cooking_time'.");
            return null!;
        }

        private IActionResult ValidateImportRecipe(RecipeXmlImport importRecipe)
        {
            if (importRecipe == null)
                return BadRequest("No XML content provided.");
            if (string.IsNullOrWhiteSpace(importRecipe.Title) || importRecipe.CookingTime <= 0)
                return BadRequest("Validation error: Title or cooking time is missing or invalid.");
            return null!;
        }

        private object RecipeToResult(Recipe recipe)
        {
            return new
            {
                hash = recipe.Hash,
                title = recipe.Title,
                description = recipe.Description,
                categories = recipe.Categories.Select(c => c.Name).ToList(),
                cooking_time = recipe.CookingTime
            };
        }

        [HttpGet]
        public IActionResult GetRecipes(
            [FromQuery] int offset = 0,
            [FromQuery] int? count = null,
            [FromQuery] string order_by = "title",
            [FromQuery] string order = "asc",
            [FromQuery] List<string>? categories = null)
        {
            var countValidation = ValidateCount(count);
            if (countValidation != null) return countValidation;

            var orderByValidation = ValidateOrderBy(order_by);
            if (orderByValidation != null) return orderByValidation;

            var query = _context.Recipes
                .Include(r => r.Categories)
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
                .AsQueryable();

            if (categories != null && categories.Count > 0)
            {
                var categorySet = categories.ToHashSet(System.StringComparer.OrdinalIgnoreCase);
                query = query.Where(r => r.Categories.Any(cat => categorySet.Contains(cat.Name)));
            }

            query = order_by switch
            {
                "title" => order == "desc" ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
                "cooking_time" => order == "desc" ? query.OrderByDescending(r => r.CookingTime) : query.OrderBy(r => r.CookingTime),
                _ => query
            };

            var recipes = query
                .Skip(offset)
                .Take(Math.Min(count ?? 0, 25))
                .ToList()
                .Select(RecipeToResult)
                .ToList();

            return Ok(new { recipes });
        }

        [HttpPost]
        [Consumes("application/xml")]
        public IActionResult UploadRecipe(
            [FromHeader] string uuid,
            [FromBody] RecipeXmlImport importRecipe)
        {
            var validation = ValidateImportRecipe(importRecipe);
            if (validation != null) return validation;

            // Prüfe auf doppelten Hash
            if (!string.IsNullOrWhiteSpace(importRecipe.Hash) && _context.Recipes.Any(r => r.Hash == importRecipe.Hash))
            {
                return Conflict($"A recipe with the hash '{importRecipe.Hash}' already exists.");
            }

            if (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
                EnsureCategoriesExist(importRecipe.Categories);

            var recipe = _mapper.Map<Recipe>(importRecipe);
            recipe.OwnerUuid = uuid;

            // Setze die Rückreferenz für jede Instruction (wichtig für EF Core)
            if (recipe.Instructions != null)
            {
                foreach (var instruction in recipe.Instructions)
                {
                    instruction.Recipe = recipe;
                    if (instruction.Ingredients != null)
                    {
                        foreach (var ingredient in instruction.Ingredients)
                        {
                            ingredient.Instruction = instruction;
                        }
                    }
                }
            }

            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            recipe.Categories = (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
                ? GetCategoriesFromDb(importRecipe.Categories)
                : new List<Category>();

            _context.Recipes.Update(recipe);
            _context.SaveChanges();

            return Ok(RecipeToResult(recipe));
        }

        [HttpPut("{hash}")]
        [Consumes("application/xml")]
        public IActionResult UpdateRecipe(string hash, [FromHeader] string uuid, [FromBody] RecipeXmlImport importRecipe)
        {
            var validation = ValidateImportRecipe(importRecipe);
            if (validation != null) return validation;

            var existingRecipe = _context.Recipes
                .Include(r => r.Categories)
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
                .FirstOrDefault(r => r.Hash == hash);

            if (existingRecipe == null)
                return BadRequest($"No recipe found with the specified hash '{hash}'.");

            if (existingRecipe.OwnerUuid != uuid)
                return BadRequest("Wrong user: You are not authorized to update this recipe.");

            if (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
                EnsureCategoriesExist(importRecipe.Categories);

            existingRecipe.Title = importRecipe.Title;
            existingRecipe.Description = importRecipe.Description;
            existingRecipe.CookingTime = importRecipe.CookingTime;
            existingRecipe.ImageName = importRecipe.ImageName;
            existingRecipe.Servings = importRecipe.Servings;

            existingRecipe.Categories = (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
                ? GetCategoriesFromDb(importRecipe.Categories)
                : new List<Category>();

            // --- Instructions & Ingredients ersetzen ---
            if (importRecipe.Instructions != null)
            {
                var oldInstructions = existingRecipe.Instructions?.ToList() ?? new List<Instruction>();
                foreach (var instr in oldInstructions)
                {
                    if (instr.Ingredients != null)
                        _context.Ingredients.RemoveRange(instr.Ingredients);
                    _context.Instructions.Remove(instr);
                }
                existingRecipe.Instructions.Clear();
                _context.SaveChanges();

                var newInstructions = _mapper.Map<List<Instruction>>(importRecipe.Instructions);
                foreach (var instruction in newInstructions)
                {
                    instruction.Recipe = existingRecipe;
                    if (instruction.Ingredients != null)
                    {
                        foreach (var ingredient in instruction.Ingredients)
                        {
                            ingredient.Instruction = instruction;
                        }
                    }
                    existingRecipe.Instructions.Add(instruction);
                }
                _context.SaveChanges();
            }

            _context.Recipes.Update(existingRecipe);
            _context.SaveChanges();

            _context.Entry(existingRecipe)
                .Collection(r => r.Instructions)
                .Query()
                .Include(i => i.Ingredients)
                .Load();

            return Ok(RecipeToResult(existingRecipe));
        }

        [HttpGet("{hash}")]
        [Produces("application/xml")]
        public IActionResult GetRecipeByHash(string hash)
        {
            var recipe = _context.Recipes
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
                .Include(r => r.Categories)
                .FirstOrDefault(r => r.Hash == hash);
            if (recipe == null)
                return NotFound($"Recipe with hash '{hash}' not found.");

            // Mixed Content Mapping: DB-Modell -> XML-DTO
            var xmlInstructions = new List<InstructionXmlDto>();
            if (recipe.Instructions != null)
            {
                foreach (var instr in recipe.Instructions)
                {
                    xmlInstructions.Add(instr.ToXmlDto());
                }
            }

            // Für die XML-Ausgabe: Erzeuge ein separates Objekt mit List<InstructionXmlDto>
            var xmlRecipe = new RecipeXmlExport
            {
                Hash = recipe.Hash,
                Title = recipe.Title,
                ImageName = recipe.ImageName,
                Description = recipe.Description,
                Servings = recipe.Servings,
                CookingTime = recipe.CookingTime,
                Categories = recipe.Categories?.Select(c => c.Name).ToList() ?? new List<string>(),
                Instructions = xmlInstructions
            };

            var serializer = new XmlSerializer(typeof(RecipeXmlExport));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };
            using var stringWriter = new Utf8StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            serializer.Serialize(xmlWriter, xmlRecipe, ns);
            var xml = stringWriter.ToString();

            return Content(xml, "application/xml");
        }
    }

    // Hilfs-DTO für XML-Export (damit Instructions als List<InstructionXmlDto> serialisiert werden)
    [XmlRoot("recipe")]
    public class RecipeXmlExport
    {
        [XmlElement("hash")]
        public string Hash { get; set; } = string.Empty;

        [XmlElement("title")]
        public string Title { get; set; } = string.Empty;

        [XmlElement("imageName")]
        public string ImageName { get; set; } = string.Empty;

        [XmlElement("description")]
        public string Description { get; set; } = string.Empty;

        [XmlElement("servings")]
        public int? Servings { get; set; }

        [XmlElement("cookingTime")]
        public int CookingTime { get; set; }

        [XmlArray("categories")]
        [XmlArrayItem("category")]
        public List<string> Categories { get; set; } = new();

        [XmlArray("instructions")]
        [XmlArrayItem("instruction")]
        public List<InstructionXmlDto> Instructions { get; set; } = new();
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}