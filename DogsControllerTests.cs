using AutoMapper;
using DogHouse.Api.Controllers;
using DogHouse.Api.DTOs;
using DogHouse.Api.Mapping;
using DogHouse.Api.Models;
using DogHouse.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DogHouse.Tests
{
    public class DogsControllerTests
    {
        private IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(cfg => cfg.AddProfile<DogProfile>());
            return cfg.CreateMapper();
        }

        [Fact]
        public async Task GetDogs_ReturnsOkWithDtos()
        {
            var mock = new Mock<IDogService>();
            mock.Setup(s => s.GetDogsAsync(null, null, 1, 100))
                .ReturnsAsync((new List<Dog> { new Dog { Name = "n", Color = "c", TailLength = 1, Weight = 1 } }, 1));

            var controller = new DogsController(mock.Object, CreateMapper());

            var result = await controller.GetDogs(null, null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<DogDto>>(ok.Value);
            Assert.Single(list);
            Assert.Equal("n", list[0].Name);
        }

        [Fact]
        public async Task CreateDog_InvalidJson_ReturnsBadRequest()
        {
            var mock = new Mock<IDogService>();
            var controller = new DogsController(mock.Object, CreateMapper());

            var result = await controller.CreateDog(null);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateDog_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            var mock = new Mock<IDogService>();
            mock.Setup(s => s.CreateDogAsync(It.IsAny<CreateDogDto>()))
                .ThrowsAsync(new ArgumentException("Name is required"));

            var controller = new DogsController(mock.Object, CreateMapper());

            var dto = new CreateDogDto { Name = " ", Color = "c", TailLength = 1, Weight = 1 };
            var result = await controller.CreateDog(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateDog_ServiceThrowsInvalidOperation_ReturnsConflict()
        {
            var mock = new Mock<IDogService>();
            mock.Setup(s => s.CreateDogAsync(It.IsAny<CreateDogDto>()))
                .ThrowsAsync(new InvalidOperationException("exists"));

            var controller = new DogsController(mock.Object, CreateMapper());

            var dto = new CreateDogDto { Name = "n", Color = "c", TailLength = 1, Weight = 1 };
            var result = await controller.CreateDog(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
        }
    }
}
