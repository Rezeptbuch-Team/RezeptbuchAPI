using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using RezeptbuchAPI.Controllers;
using RezeptbuchAPI.Models;
using RezeptbuchAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace RezeptbuchAPI.Tests
{
    [TestFixture]
    public class RecipesControllerTests
    {
        private RecipeBookContext _context;
        private RecipesController _controller;
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<RecipeBookContext>()
                .UseInMemoryDatabase(databaseName: "RecipesTestDb")
                .Options;
            _context = new RecipeBookContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            // Mapper-Konfiguration für Tests
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<RecipeMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _controller = new RecipesController(_context, _mapper);
        }

        [Test]
        public void GetRecipes_ReturnsBadRequest_WhenCountIsNull()
        {
            var result = _controller.GetRecipes(count: null);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetRecipes_ReturnsBadRequest_WhenCountIsTooHigh()
        {
            var result = _controller.GetRecipes(count: 30);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetRecipes_ReturnsBadRequest_WhenOrderByInvalid()
        {
            var result = _controller.GetRecipes(count: 5, order_by: "invalid");
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadRecipe_ReturnsBadRequest_WhenImportRecipeIsNull()
        {
            // Der zweite Parameter ist nullable, daher explizit null zulassen
            var result = _controller.UploadRecipe("uuid", null!);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadRecipe_ReturnsBadRequest_WhenTitleOrCookingTimeInvalid()
        {
            var import = new RecipeXmlImport { Title = "", CookingTime = 0 };
            var result = _controller.UploadRecipe("uuid", import);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadRecipe_ReturnsOk_WhenValidRecipe()
        {
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            var result = _controller.UploadRecipe("uuid", import);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public void UploadRecipe_ReturnsConflict_WhenHashExists()
        {
            var import = new RecipeXmlImport
            {
                Hash = "irgendeinTestHash",
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid", import);
            var result = _controller.UploadRecipe("uuid", import);
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public void GetRecipeByHash_ReturnsNotFound_WhenRecipeDoesNotExist()
        {
            var result = _controller.GetRecipeByHash("notfound");
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public void GetRecipeByHash_ReturnsXml_WhenRecipeExists()
        {
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid", import);
            var hash = _context.Recipes.First().Hash;
            var result = _controller.GetRecipeByHash(hash);
            Assert.That(result, Is.InstanceOf<ContentResult>());
            var contentResult = result as ContentResult;
            Assert.That(contentResult, Is.Not.Null);
            Assert.That(contentResult!.ContentType, Is.EqualTo("application/xml"));
        }

        [Test]
        public void UpdateRecipe_ReturnsBadRequest_WhenRecipeNotFound()
        {
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            var result = _controller.UpdateRecipe("notfound", "uuid", import);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UpdateRecipe_ReturnsBadRequest_WhenWrongUser()
        {
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid1", import);
            var hash = _context.Recipes.First().Hash;
            var result = _controller.UpdateRecipe(hash, "uuid2", import);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UpdateRecipe_ReturnsOk_WhenValidUpdate()
        {
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid", import);
            var hash = _context.Recipes.First().Hash;
            var update = new RecipeXmlImport
            {
                Title = "Test2",
                CookingTime = 20,
                Description = "Beschreibung2",
                ImageName = "bild2.jpg",
                Categories = new List<string> { "Hauptgericht" },
                Instructions = new List<InstructionXmlDto>()
            };
            var result = _controller.UpdateRecipe(hash, "uuid", update);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public void GetRecipes_ReturnsRecipes_FilteredByCategory()
        {
            // Arrange
            var import1 = new RecipeXmlImport
            {
                Title = "Suppe",
                CookingTime = 10,
                Description = "Beschreibung1",
                ImageName = "bild1.jpg",
                Categories = new List<string> { "Suppe" },
                Instructions = new List<InstructionXmlDto>()
            };
            var import2 = new RecipeXmlImport
            {
                Title = "Salat",
                CookingTime = 5,
                Description = "Beschreibung2",
                ImageName = "bild2.jpg",
                Categories = new List<string> { "Salat" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid", import1);
            _controller.UploadRecipe("uuid", import2);

            // Act
            var result = _controller.GetRecipes(count: 10, categories: new List<string> { "Salat" }) as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var recipesProp = data?.GetType().GetProperty("recipes");
            var recipes = recipesProp?.GetValue(data) as IEnumerable<object>;
            Assert.That(recipes, Is.Not.Null);
            Assert.That(recipes!.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetRecipes_ReturnsRecipes_OrderedByCookingTimeDesc()
        {
            // Arrange
            var import1 = new RecipeXmlImport
            {
                Title = "A",
                CookingTime = 10,
                Description = "BeschreibungA",
                ImageName = "bildA.jpg",
                Categories = new List<string> { "Test" },
                Instructions = new List<InstructionXmlDto>()
            };
            var import2 = new RecipeXmlImport
            {
                Title = "B",
                CookingTime = 20,
                Description = "BeschreibungB",
                ImageName = "bildB.jpg",
                Categories = new List<string> { "Test" },
                Instructions = new List<InstructionXmlDto>()
            };
            _controller.UploadRecipe("uuid", import1);
            _controller.UploadRecipe("uuid", import2);

            // Act
            var result = _controller.GetRecipes(count: 10, order_by: "cooking_time", order: "desc") as OkObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var recipesProp = data?.GetType().GetProperty("recipes");
            var recipes = recipesProp?.GetValue(data) as IEnumerable<object>;
            Assert.That(recipes, Is.Not.Null);

            // Prüfe auf snake_case Property
            var firstRecipe = recipes!.First();
            var dict = firstRecipe as IDictionary<string, object>;
            if (dict != null)
            {
                Assert.That(dict["cooking_time"], Is.EqualTo(20));
            }
            else
            {
                // Fallback für ExpandoObject/dynamic
                var cookingTime = firstRecipe.GetType().GetProperty("cooking_time")?.GetValue(firstRecipe);
                Assert.That(cookingTime, Is.EqualTo(20));
            }
        }

        [Test]
        public void UploadRecipe_CreatesNewCategory_IfNotExists()
        {
            // Arrange
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "NeueKategorie" },
                Instructions = new List<InstructionXmlDto>()
            };

            // Act
            _controller.UploadRecipe("uuid", import);

            // Assert
            Assert.That(_context.Categories.Any(c => c.Name == "NeueKategorie"), Is.True);
        }

        [Test]
        public void UpdateRecipe_UpdatesInstructionsAndIngredients()
        {
            // Arrange
            var instr1 = new InstructionXmlDto
            {
                Content = new List<object> { "Text1" }
            };
            var import = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Test" },
                Instructions = new List<InstructionXmlDto> { instr1 }
            };
            _controller.UploadRecipe("uuid", import);
            var hash = _context.Recipes.First().Hash;

            var instr2 = new InstructionXmlDto
            {
                Content = new List<object> { "Text2" }
            };
            var update = new RecipeXmlImport
            {
                Title = "Test",
                CookingTime = 10,
                Description = "Beschreibung2",
                ImageName = "bild2.jpg",
                Categories = new List<string> { "Test" },
                Instructions = new List<InstructionXmlDto> { instr2 }
            };

            // Act
            _controller.UpdateRecipe(hash, "uuid", update);

            // Assert
            var recipe = _context.Recipes.Include(r => r.Instructions).First(r => r.Hash == hash);
            Assert.That(recipe.Instructions.Count, Is.EqualTo(1));
            Assert.That(recipe.Instructions.First().Text, Does.Contain("Text2"));
        }

        [Test]
        public void GetRecipes_ReturnsEmptyList_WhenNoRecipesExist()
        {
            var result = _controller.GetRecipes(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var recipesProp = data?.GetType().GetProperty("recipes");
            var recipes = recipesProp?.GetValue(data) as IEnumerable<object>;
            Assert.That(recipes, Is.Not.Null);
            Assert.That(recipes!.Count(), Is.EqualTo(0));
        }

        [Test]
        public void UploadRecipe_AllowsEmptyCategoriesAndInstructions()
        {
            var import = new RecipeXmlImport
            {
                Title = "Ohne Kategorien und Anweisungen",
                CookingTime = 5,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string>(),
                Instructions = new List<InstructionXmlDto>()
            };
            var result = _controller.UploadRecipe("uuid", import);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }

        [Test]
        public void UploadRecipe_AndGetRecipeByHash_ValidatesReturnedContent()
        {
            var import = new RecipeXmlImport
            {
                Title = "Validierung",
                CookingTime = 15,
                Description = "Beschreibung",
                ImageName = "bild.jpg",
                Categories = new List<string> { "Test" },
                Instructions = new List<InstructionXmlDto>()
            };
            var uploadResult = _controller.UploadRecipe("uuid", import) as OkObjectResult;
            Assert.That(uploadResult, Is.Not.Null);

            // Anpassung: Verwende Reflection, da Rückgabe ein anonymes Objekt ist
            var value = uploadResult!.Value;
            Assert.That(value, Is.Not.Null);

            var type = value.GetType();
            Assert.That(type.GetProperty("title"), Is.Not.Null);
            Assert.That(type.GetProperty("cooking_time"), Is.Not.Null);
            Assert.That(type.GetProperty("categories"), Is.Not.Null);

            Assert.That(type.GetProperty("title")!.GetValue(value), Is.EqualTo("Validierung"));
            Assert.That(type.GetProperty("cooking_time")!.GetValue(value), Is.EqualTo(15));
            var categories = type.GetProperty("categories")!.GetValue(value) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories!, Contains.Item("Test"));

            // Jetzt per Hash abrufen und prüfen
            var hash = type.GetProperty("hash")!.GetValue(value) as string;
            Assert.That(hash, Is.Not.Null);
            var getResult = _controller.GetRecipeByHash(hash!) as ContentResult;
            Assert.That(getResult, Is.Not.Null);
            Assert.That(getResult!.Content, Does.Contain("Validierung"));
        }
    }
}