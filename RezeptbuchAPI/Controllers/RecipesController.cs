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

namespace RezeptbuchAPI.Controllers
{
    [ApiController]
    [Route("recipes")]
    public class RecipesController : ControllerBase
    {
        private readonly RecipeBookContext _context;

        public RecipesController(RecipeBookContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetRecipes(
            [FromQuery] int offset = 0,
            [FromQuery] int? count = null,
            [FromQuery] string order_by = "title",
            [FromQuery] string order = "asc",
            [FromQuery] List<string>? categories = null)
        {
            if (count == null)
                return BadRequest("Parameter 'count' ist erforderlich.");
            if (count > 25)
                return BadRequest("Parameter 'count' darf maximal 25 sein.");

            if (order_by != "title" && order_by != "cooking_time")
            {
                return BadRequest("Invalid value for 'order_by'. Must be 'title' or 'cooking_time'.");
            }

            var query = _context.Recipes
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
                .AsQueryable();

            if (categories != null && categories.Count > 0)
            {
                query = query.Where(r => r.Categories.Any(c => categories.Contains(c)));
            }

            query = order_by switch
            {
                "title" => order == "desc" ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
                "cooking_time" => order == "desc" ? query.OrderByDescending(r => r.CookingTime) : query.OrderBy(r => r.CookingTime),
                _ => query
            };

            var recipes = query
                .Skip(offset)
                .Take(Math.Min(count.Value, 25))
                .ToList()
                .Select(r => new RecipeDto
                {
                    Hash = r.Hash,
                    Title = r.Title,
                    Description = r.Description,
                    Categories = r.Categories,
                    CookingTime = r.CookingTime,
                    ImageName = r.ImageName,
                    Servings = r.Servings,
                    Instructions = r.Instructions?.Select(InstructionDto.FromEntity).ToList() ?? new()
                })
                .ToList();

            return Ok(new { recipes });
        }

        [HttpPost]
        [Consumes("application/xml")]
        public IActionResult UploadRecipe(
    [FromHeader] string uuid,
    [FromBody] RecipeXmlImport importRecipe)
        {
            if (importRecipe == null)
                return BadRequest("No XML content provided.");

            if (string.IsNullOrWhiteSpace(importRecipe.Title) || importRecipe.CookingTime <= 0)
                return BadRequest("Validation error: Title or cooking time is missing or invalid.");

            // Kategorien prüfen und ggf. anlegen
            if (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
            {
                var existingCategories = _context.Categories
                    .Select(c => c.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newCategories = importRecipe.Categories
                    .Where(cat => !existingCategories.Contains(cat))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var cat in newCategories)
                {
                    _context.Categories.Add(new Category { Name = cat });
                }
                if (newCategories.Count > 0)
                    _context.SaveChanges();
            }

            // Mapping ausgelagert
            var recipe = RecipeImportMapper.MapToRecipe(importRecipe, uuid);

            // Prüfe auf doppelten Hash
            if (_context.Recipes.Any(r => r.Hash == recipe.Hash))
            {
                return Conflict($"A recipe with the hash '{recipe.Hash}' already exists.");
            }

            // Instructions in den Kontext aufnehmen
            if (recipe.Instructions != null)
            {
                foreach (var instruction in recipe.Instructions)
                {
                    _context.Instructions.Add(instruction);
                }
            }

            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var dto = new RecipeDto
            {
                Hash = recipe.Hash,
                Title = recipe.Title,
                Description = recipe.Description,
                Categories = recipe.Categories,
                CookingTime = recipe.CookingTime,
                ImageName = recipe.ImageName,
                Servings = recipe.Servings,
                Instructions = recipe.Instructions?.Select(InstructionDto.FromEntity).ToList() ?? new()
            };

            return Ok(dto);
        }

        [HttpGet("{hash}")]
        [Produces("application/xml")]
        public IActionResult GetRecipeByHash(string hash)
        {
            var recipe = _context.Recipes
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
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
                Categories = recipe.Categories,
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

        [HttpPut("{hash}")]
        [Consumes("application/xml")]
        public IActionResult UpdateRecipe(string hash, [FromHeader] string uuid, [FromBody] RecipeXmlImport importRecipe)
        {
            if (importRecipe == null)
                return BadRequest("No XML content provided.");

            if (string.IsNullOrWhiteSpace(importRecipe.Title) || importRecipe.CookingTime <= 0)
                return BadRequest("Validation error: Title or cooking time is missing or invalid.");

            var existingRecipe = _context.Recipes
                .Include(r => r.Instructions)
                .ThenInclude(i => i.Ingredients)
                .FirstOrDefault(r => r.Hash == hash);

            if (existingRecipe == null)
                return BadRequest($"No recipe found with the specified hash '{hash}'.");

            if (existingRecipe.OwnerUuid != uuid)
                return BadRequest("Wrong user: You are not authorized to update this recipe.");

            // Kategorien prüfen und ggf. anlegen (wie beim POST)
            if (importRecipe.Categories != null && importRecipe.Categories.Count > 0)
            {
                var existingCategories = _context.Categories
                    .Select(c => c.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newCategories = importRecipe.Categories
                    .Where(cat => !existingCategories.Contains(cat))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var cat in newCategories)
                {
                    _context.Categories.Add(new Category { Name = cat });
                }
                if (newCategories.Count > 0)
                    _context.SaveChanges();
            }

            // Felder aktualisieren
            existingRecipe.Title = importRecipe.Title;
            existingRecipe.Description = importRecipe.Description;
            existingRecipe.CookingTime = importRecipe.CookingTime;
            existingRecipe.Categories = importRecipe.Categories ?? new List<string>();
            existingRecipe.ImageName = importRecipe.ImageName;
            existingRecipe.Servings = importRecipe.Servings;

            // --- Instructions & Ingredients ersetzen ---
            if (importRecipe.Instructions != null)
            {
                // 1. Alte Instructions inkl. Ingredients entfernen
                var oldInstructions = _context.Instructions
                    .Where(i => i.RecipeId == existingRecipe.GetHashCode()) // FALSCH: RecipeId ist int, Hash ist string!
                    .Include(i => i.Ingredients)
                    .ToList();

                // Korrektur: Wir müssen alle Instructions des Rezepts anhand der Navigation löschen!
                oldInstructions = existingRecipe.Instructions?.ToList() ?? new List<Instruction>();

                foreach (var instr in oldInstructions)
                {
                    if (instr.Ingredients != null)
                        _context.Ingredients.RemoveRange(instr.Ingredients);

                    _context.Instructions.Remove(instr);
                }
                _context.SaveChanges();

                // 2. Neue Instructions aus dem Import anlegen
                foreach (var instrDto in importRecipe.Instructions)
                {
                    var instruction = Instruction.FromXmlDto(instrDto);
                    instruction.Recipe = existingRecipe;
                    // RecipeId wird von EF gesetzt, Recipe ist gesetzt
                    if (instruction.Ingredients != null)
                    {
                        foreach (var ingredient in instruction.Ingredients)
                        {
                            ingredient.Instruction = instruction;
                        }
                    }
                    _context.Instructions.Add(instruction);
                }
                _context.SaveChanges();
            }

            _context.Recipes.Update(existingRecipe);
            _context.SaveChanges();

            // Navigation neu laden, damit Instructions im DTO aktuell sind
            _context.Entry(existingRecipe)
                .Collection(r => r.Instructions)
                .Query()
                .Include(i => i.Ingredients)
                .Load();

            var dto = new RecipeDto
            {
                Hash = existingRecipe.Hash,
                Title = existingRecipe.Title,
                Description = existingRecipe.Description,
                Categories = existingRecipe.Categories,
                CookingTime = existingRecipe.CookingTime,
                ImageName = existingRecipe.ImageName,
                Servings = existingRecipe.Servings,
                Instructions = existingRecipe.Instructions?.Select(InstructionDto.FromEntity).ToList() ?? new()
            };
            return Ok(dto);
        }
    }

    // Hilfs-DTO für XML-Export (damit Instructions als List<InstructionXmlDto> serialisiert werden)
    [XmlRoot("recipe")]
    public class RecipeXmlExport
    {
        [XmlElement("hash")]
        public string Hash { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("imageName")]
        public string ImageName { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

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