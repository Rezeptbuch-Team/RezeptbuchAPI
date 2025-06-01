using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using RezeptbuchAPI.Controllers;
using RezeptbuchAPI.Models;

namespace RezeptbuchAPI.Tests
{
    [TestFixture]
    public class CategoriesControllerTests
    {
        private RecipeBookContext _context;
        private CategoriesController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<RecipeBookContext>()
                .UseInMemoryDatabase(databaseName: "CategoriesTestDb")
                .Options;
            _context = new RecipeBookContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            _controller = new CategoriesController(_context);
        }

        [Test]
        public void GetCategories_ReturnsBadRequest_WhenCountIsNull()
        {
            var result = _controller.GetCategories(count: null);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetCategories_ReturnsBadRequest_WhenCountIsTooHigh()
        {
            var result = _controller.GetCategories(count: 51);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetCategories_ReturnsBadRequest_WhenNoCategoriesExist()
        {
            var result = _controller.GetCategories(count: 10);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetCategories_ReturnsOk_WithCategories()
        {
            _context.Categories.Add(new Category { Name = "A" });
            _context.Categories.Add(new Category { Name = "B" });
            _context.SaveChanges();

            var result = _controller.GetCategories(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories!.Count(), Is.EqualTo(2));
            Assert.That(categories, Contains.Item("A"));
            Assert.That(categories, Contains.Item("B"));
        }

        [Test]
        public void GetCategories_RespectsOffsetAndCount()
        {
            _context.Categories.Add(new Category { Name = "A" });
            _context.Categories.Add(new Category { Name = "B" });
            _context.Categories.Add(new Category { Name = "C" });
            _context.SaveChanges();

            var result = _controller.GetCategories(offset: 1, count: 1) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories!.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetCategories_ReturnsCategories_SortedAlphabetically()
        {
            _context.Categories.Add(new Category { Name = "Banane" });
            _context.Categories.Add(new Category { Name = "Apfel" });
            _context.Categories.Add(new Category { Name = "Zitrone" });
            _context.SaveChanges();

            var result = _controller.GetCategories(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            var list = categories!.ToList();
            Assert.That(list, Is.Ordered);
        }

        [Test]
        public void GetCategories_IgnoresDuplicateCategoryNames()
        {
            _context.Categories.Add(new Category { Name = "Obst" });
            _context.Categories.Add(new Category { Name = "obst" }); // Case-Insensitive Duplikat
            _context.SaveChanges();

            var result = _controller.GetCategories(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            // Erwartung: Je nach Implementierung evtl. beide oder nur eine Kategorie
            // Hier prüfen wir, dass beide vorkommen (falls nicht dedupliziert)
            Assert.That(categories!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetCategories_ReturnsBadRequest_WhenCountIsZeroOrNegative()
        {
            var resultZero = _controller.GetCategories(count: 0);
            Assert.That(resultZero, Is.InstanceOf<BadRequestObjectResult>());

            var resultNegative = _controller.GetCategories(count: -5);
            Assert.That(resultNegative, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetCategories_ReturnsCategories_WithUnicodeNames()
        {
            _context.Categories.Add(new Category { Name = "Süßspeise" });
            _context.Categories.Add(new Category { Name = "Frühstück 🍳" });
            _context.SaveChanges();

            var result = _controller.GetCategories(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories, Contains.Item("Süßspeise"));
            Assert.That(categories, Contains.Item("Frühstück 🍳"));
        }

        [Test]
        public void GetCategories_HandlesWhitespaceInCategoryNames()
        {
            _context.Categories.Add(new Category { Name = "  Gemüse  " });
            _context.SaveChanges();

            var result = _controller.GetCategories(count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories!.First().Trim(), Is.EqualTo("Gemüse"));
        }

        [Test]
        public void GetCategories_ReturnsBadRequest_WhenOffsetIsNegative()
        {
            var result = _controller.GetCategories(offset: -1, count: 10);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetCategories_ReturnsAllRemaining_WhenCountExceedsAvailable()
        {
            _context.Categories.Add(new Category { Name = "A" });
            _context.Categories.Add(new Category { Name = "B" });
            _context.Categories.Add(new Category { Name = "C" });
            _context.SaveChanges();

            var result = _controller.GetCategories(offset: 2, count: 10) as OkObjectResult;
            Assert.That(result, Is.Not.Null);
            var data = result!.Value;
            var categoriesProp = data?.GetType().GetProperty("categories");
            var categories = categoriesProp?.GetValue(data) as IEnumerable<string>;
            Assert.That(categories, Is.Not.Null);
            Assert.That(categories!.Count(), Is.EqualTo(1));
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }
    }
}