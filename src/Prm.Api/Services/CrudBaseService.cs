using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public abstract class CrudBaseService<TEntity, TKey, TDto, TCreateRequest, TUpdateRequest>(
    ICrudBaseRepository<TEntity, TKey> _repository,
    IMapper _mapper,
    string _notFoundMessage) : ICrudBaseService<TDto, TKey, TCreateRequest, TUpdateRequest>
    where TEntity : class
{
    protected ICrudBaseRepository<TEntity, TKey> Repository { get; } = _repository;
    protected IMapper Mapper { get; } = _mapper;
    protected string NotFoundMessage { get; } = _notFoundMessage;

    public virtual async Task<TDto> Get(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrow(id, cancellationToken);
        return Mapper.Map<TDto>(entity);
    }

    public virtual async Task<IReadOnlyList<TDto>> GetAll(CancellationToken cancellationToken = default)
    {
        var entities = await Repository.GetAll(cancellationToken);
        return Mapper.Map<IReadOnlyList<TDto>>(entities);
    }

    public virtual async Task<TKey> Add(TCreateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = Mapper.Map<TEntity>(request);
        await Repository.Add(entity, cancellationToken);
        await Repository.SaveChanges(cancellationToken);
        return GetEntityKey(entity);
    }

    protected virtual TKey GetEntityKey(TEntity entity) =>
        throw new NotSupportedException($"Override {nameof(Add)} or {nameof(GetEntityKey)} in the derived service.");

    public virtual async Task<bool> Update(TKey id, TUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrow(id, cancellationToken);
        Mapper.Map(request, entity);
        Repository.Update(entity);
        await Repository.SaveChanges(cancellationToken);
        return true;
    }

    public virtual async Task Delete(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityOrThrow(id, cancellationToken);
        Repository.Remove(entity);
        await Repository.SaveChanges(cancellationToken);
    }

    protected async Task<TEntity> GetEntityOrThrow(TKey id, CancellationToken cancellationToken) =>
        await Repository.GetById(id, cancellationToken)
        ?? throw new KeyNotFoundException(NotFoundMessage);
}
