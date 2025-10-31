using AutoMapper;
using DogHouse.Api.Data;
using DogHouse.Api.DTOs;
using DogHouse.Api.Mapping;
using DogHouse.Api.Models;
using DogHouse.Api.Repositories;
using DogHouse.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DogHouse.Tests
{
    public class DogServiceTests
    {
        private DogHouseContext CreateContext(string name)
        {
            var options = new DbContextOptionsBuilder<DogHouseContext>()
                .UseInMemoryDatabase(databaseName: name)
                .Options;
            return new DogHouseContext(options);
        }

        private IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(cfg => cfg.AddProfile<DogProfile>());
            return cfg.CreateMapper();
        }

        [Fact]
        public async Task GetDogs_DefaultOrdering_ReturnsOrderedByName()
        {
            using var ctx = CreateContext("GetDogs_DefaultOrdering");
            ctx.Dogs.AddRange(new[] {
                new Dog { Name = "B", Color = "red", TailLength = 1, Weight = 1 },
                new Dog { Name = "A", Color = "blue", TailLength = 2, Weight = 2 },
                new Dog { Name = "C", Color = "green", TailLength = 3, Weight = 3 }
            });
            ctx.SaveChanges();

            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var (items, total) = await svc.GetDogsAsync(null, null, 1, 10);

            Assert.Equal(3, total);
            Assert.Equal(new[] { "A", "B", "C" }, items.Select(d => d.Name).ToArray());
        }

        [Fact]
        public async Task GetDogs_OrderByWeightDesc_ReturnsOrderedByWeightDesc()
        {
            using var ctx = CreateContext("GetDogs_OrderByWeightDesc");
            ctx.Dogs.AddRange(new[] {
                new Dog { Name = "one", Color = "c", TailLength = 1, Weight = 1 },
                new Dog { Name = "two", Color = "c", TailLength = 2, Weight = 10 },
                new Dog { Name = "three", Color = "c", TailLength = 3, Weight = 5 }
            });
            ctx.SaveChanges();

            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var (items, total) = await svc.GetDogsAsync("weight", "desc", 1, 10);

            Assert.Equal(3, total);
            Assert.Equal(new[] { 10, 5, 1 }, items.Select(d => d.Weight).ToArray());
        }

        [Fact]
        public async Task GetDogs_InvalidAttribute_ThrowsArgumentException()
        {
            using var ctx = CreateContext("GetDogs_InvalidAttribute");
            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await svc.GetDogsAsync("unknown", "asc", 1, 10));
        }

        [Fact]
        public async Task GetDogs_Paging_Works()
        {
            using var ctx = CreateContext("GetDogs_Paging");
            for (int i = 0; i < 25; i++)
            {
                ctx.Dogs.Add(new Dog { Name = i.ToString(), Color = "c", TailLength = i, Weight = i });
            }
            ctx.SaveChanges();

            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var (items, total) = await svc.GetDogsAsync(null, null, pageNumber: 2, pageSize: 10);

            Assert.Equal(25, total);
            Assert.Equal(10, items.Count);
        }

        [Fact]
        public async Task CreateDog_NullDto_ThrowsArgumentNullException()
        {
            using var ctx = CreateContext("CreateDog_NullDto");
            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await svc.CreateDogAsync(null));
        }

        [Fact]
        public async Task CreateDog_MissingName_ThrowsArgumentException()
        {
            using var ctx = CreateContext("CreateDog_MissingName");
            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var dto = new CreateDogDto { Name = " ", Color = "c", TailLength = 1, Weight = 1 };
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await svc.CreateDogAsync(dto));
        }

        [Fact]
        public async Task CreateDog_NegativeValues_ThrowsArgumentException()
        {
            using var ctx = CreateContext("CreateDog_NegativeValues");
            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var dto1 = new CreateDogDto { Name = "n1", Color = "c", TailLength = -1, Weight = 1 };
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await svc.CreateDogAsync(dto1));

            var dto2 = new CreateDogDto { Name = "n2", Color = "c", TailLength = 1, Weight = -5 };
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await svc.CreateDogAsync(dto2));
        }

        [Fact]
        public async Task CreateDog_DuplicateName_ThrowsInvalidOperationException()
        {
            using var ctx = CreateContext("CreateDog_DuplicateName");
            ctx.Dogs.Add(new Dog { Name = "dup", Color = "c", TailLength = 1, Weight = 1 });
            ctx.SaveChanges();

            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var dto = new CreateDogDto { Name = "dup", Color = "c", TailLength = 1, Weight = 1 };
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await svc.CreateDogAsync(dto));
        }

        [Fact]
        public async Task CreateDog_Success_AddsDog()
        {
            using var ctx = CreateContext("CreateDog_Success");
            var repo = new DogRepository(ctx);
            var svc = new DogService(repo, CreateMapper());

            var dto = new CreateDogDto { Name = "newdog", Color = "c", TailLength = 4, Weight = 8 };
            await svc.CreateDogAsync(dto);

            var saved = await ctx.Dogs.FindAsync("newdog");
            Assert.NotNull(saved);
            Assert.Equal(4, saved.TailLength);
            Assert.Equal(8, saved.Weight);
        }
    }
}
