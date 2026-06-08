using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Tests.Services;

public class CrudBaseServiceTests
{
    private readonly Mock<ICrudBaseRepository<TestEntity, int>> _repository = new();
    private readonly Mock<IMapper> _mapper = new();

    public CrudBaseServiceTests()
    {
        _mapper
            .Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns((TestEntity entity) => new TestDto { Id = entity.Id, Name = entity.Name });
        _mapper
            .Setup(x => x.Map<TestEntity>(It.IsAny<TestCreateRequest>()))
            .Returns((TestCreateRequest request) => new TestEntity { Name = request.Name });
        _mapper
            .Setup(x => x.Map(It.IsAny<TestUpdateRequest>(), It.IsAny<TestEntity>()))
            .Callback<TestUpdateRequest, TestEntity>((request, entity) => entity.Name = request.Name);
        _mapper
            .Setup(x => x.Map<IReadOnlyList<TestDto>>(It.IsAny<IReadOnlyList<TestEntity>>()))
            .Returns((IReadOnlyList<TestEntity> entities) =>
                entities.Select(entity => new TestDto { Id = entity.Id, Name = entity.Name }).ToList());
    }

    [Fact]
    public async Task Get_WhenEntityExists_ReturnsMappedDto()
    {
        _repository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestEntity { Id = 1, Name = "Alpha" });

        var sut = CreateSut();
        var result = await sut.Get(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Alpha", result.Name);
    }

    [Fact]
    public async Task Get_WhenEntityMissing_ThrowsKeyNotFoundException()
    {
        _repository
            .Setup(x => x.GetById(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Get(99));
    }

    [Fact]
    public async Task GetAll_ReturnsMappedDtos()
    {
        _repository
            .Setup(x => x.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TestEntity { Id = 1, Name = "Alpha" }]);

        var sut = CreateSut();
        var result = await sut.GetAll();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public async Task Add_PersistsEntityAndReturnsKey()
    {
        TestEntity? saved = null;
        _repository
            .Setup(x => x.Add(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TestEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 7;
                saved = entity;
            })
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var id = await sut.Add(new TestCreateRequest { Name = "Beta" });

        Assert.Equal(7, id);
        Assert.Equal("Beta", saved!.Name);
    }

    [Fact]
    public async Task Update_WhenEntityExists_ReturnsTrue()
    {
        var entity = new TestEntity { Id = 1, Name = "Alpha" };
        _repository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var updated = await sut.Update(1, new TestUpdateRequest { Name = "Beta" });

        Assert.True(updated);
        Assert.Equal("Beta", entity.Name);
        _repository.Verify(x => x.Update(entity), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenEntityExists_RemovesEntity()
    {
        var entity = new TestEntity { Id = 1, Name = "Alpha" };
        _repository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Delete(1);

        _repository.Verify(x => x.Remove(entity), Times.Once);
    }

    private TestCrudService CreateSut() => new(_repository.Object, _mapper.Object);

    public sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestCreateRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class TestUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestCrudService(
        ICrudBaseRepository<TestEntity, int> repository,
        IMapper mapper)
        : CrudBaseService<TestEntity, int, TestDto, TestCreateRequest, TestUpdateRequest>(
            repository,
            mapper,
            "Entity not found.")
    {
        protected override int GetEntityKey(TestEntity entity) => entity.Id;
    }
}
