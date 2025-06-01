using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using RezeptbuchAPI.Controllers;
using RezeptbuchAPI.Models;
using RezeptbuchAPI.Models.DTO;

namespace RezeptbuchAPI.Tests
{
    [TestFixture]
    public class ImagesControllerTests
    {
        private RecipeBookContext _context;
        private ImagesController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<RecipeBookContext>()
                .UseInMemoryDatabase(databaseName: "ImagesTestDb")
                .Options;
            _context = new RecipeBookContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            _controller = new ImagesController(_context);
        }

        [Test]
        public void GetImageByHash_ReturnsNotFound_WhenRecipeDoesNotExist()
        {
            var result = _controller.GetImageByHash("notfound");
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public void GetImageByHash_ReturnsNotFound_WhenRecipeHasNoImage()
        {
            var recipe = new Recipe { Hash = "hash1", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _controller.GetImageByHash("hash1");
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public void GetImageByHash_ReturnsFile_WhenImageExists()
        {
            var image = new Image
            {
                Hash = "img1",
                ImageData = Encoding.UTF8.GetBytes("testdata"),
                ContentType = "image/png"
            };
            var recipe = new Recipe { Hash = "hash2", Title = "Test", OwnerUuid = "uuid", Image = image };
            _context.Images.Add(image);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _controller.GetImageByHash("hash2");
            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = result as FileContentResult;
            Assert.That(fileResult!.ContentType, Is.EqualTo("image/png"));
            Assert.That(fileResult.FileContents, Is.EqualTo(image.ImageData));
        }

        [Test]
        public void UploadImage_ReturnsBadRequest_WhenNoFile()
        {
            var recipe = new Recipe { Hash = "hash3", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var dto = new ImageUploadDto { ImageFile = null! };
            var result = _controller.UploadImage("hash3", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadImage_ReturnsBadRequest_WhenRecipeNotFound()
        {
            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("data")), 0, 4, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UploadImage("notfound", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadImage_ReturnsBadRequest_WhenWrongUser()
        {
            var recipe = new Recipe { Hash = "hash4", Title = "Test", OwnerUuid = "uuid1" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("data")), 0, 4, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UploadImage("hash4", "uuid2", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadImage_ReturnsOk_WhenValid()
        {
            var recipe = new Recipe { Hash = "hash5", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("data")), 0, 4, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UploadImage("hash5", "uuid", dto);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var updatedRecipe = _context.Recipes.Include(r => r.Image).First(r => r.Hash == "hash5");
            Assert.That(updatedRecipe.Image, Is.Not.Null);
            Assert.That(updatedRecipe.Image.ContentType, Is.EqualTo("image/png"));
        }

        [Test]
        public void UpdateImage_ReturnsBadRequest_WhenNoFile()
        {
            var recipe = new Recipe { Hash = "hash6", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var dto = new ImageUploadDto { ImageFile = null! };
            var result = _controller.UpdateImage("hash6", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UpdateImage_ReturnsBadRequest_WhenRecipeNotFound()
        {
            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("data")), 0, 4, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UpdateImage("notfound", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UpdateImage_ReturnsBadRequest_WhenWrongUser()
        {
            var recipe = new Recipe { Hash = "hash7", Title = "Test", OwnerUuid = "uuid1" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("data")), 0, 4, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UpdateImage("hash7", "uuid2", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UpdateImage_ReturnsOk_WhenValidAndImageExists()
        {
            var image = new Image
            {
                Hash = "img2",
                ImageData = Encoding.UTF8.GetBytes("old"),
                ContentType = "image/png"
            };
            var recipe = new Recipe { Hash = "hash8", Title = "Test", OwnerUuid = "uuid", Image = image };
            _context.Images.Add(image);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("newdata")), 0, 7, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UpdateImage("hash8", "uuid", dto);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var updatedRecipe = _context.Recipes.Include(r => r.Image).First(r => r.Hash == "hash8");
            Assert.That(updatedRecipe.Image.ImageData, Is.EqualTo(Encoding.UTF8.GetBytes("newdata")));
        }

        [Test]
        public void UpdateImage_ReturnsOk_WhenValidAndImageDoesNotExist()
        {
            var recipe = new Recipe { Hash = "hash9", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("imgdata")), 0, 7, "file", "test.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UpdateImage("hash9", "uuid", dto);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var updatedRecipe = _context.Recipes.Include(r => r.Image).First(r => r.Hash == "hash9");
            Assert.That(updatedRecipe.Image, Is.Not.Null);
            Assert.That(updatedRecipe.Image.ImageData, Is.EqualTo(Encoding.UTF8.GetBytes("imgdata")));
        }

        [Test]
        public void GetImageByHash_ReturnsOctetStream_WhenContentTypeIsNullOrEmpty()
        {
            var image = new Image
            {
                Hash = "img3",
                ImageData = Encoding.UTF8.GetBytes("data"),
                ContentType = null // oder ""
            };
            var recipe = new Recipe { Hash = "hash10", Title = "Test", OwnerUuid = "uuid", Image = image };
            _context.Images.Add(image);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _controller.GetImageByHash("hash10");
            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = result as FileContentResult;
            Assert.That(fileResult!.ContentType, Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void UpdateImage_ReturnsBadRequest_WhenFileIsEmpty()
        {
            var recipe = new Recipe { Hash = "hash12", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(), 0, 0, "file", "empty.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UpdateImage("hash12", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void UploadImage_ReturnsBadRequest_WhenFileIsEmpty()
        {
            var recipe = new Recipe { Hash = "hash11", Title = "Test", OwnerUuid = "uuid" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var file = new FormFile(new MemoryStream(), 0, 0, "file", "empty.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var dto = new ImageUploadDto { ImageFile = file };
            var result = _controller.UploadImage("hash11", "uuid", dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }
    }
}