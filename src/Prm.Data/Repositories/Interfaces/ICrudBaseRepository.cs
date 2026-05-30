namespace Prm.Data.Repositories.Interfaces;

public interface ICrudBaseRepository<TEntity, TKey>
    where TEntity : class
{
    Task<TEntity?> GetById(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAll(CancellationToken cancellationToken = default);
    Task Add(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<bool> Exists(TKey id, CancellationToken cancellationToken = default);
    Task SaveChanges(CancellationToken cancellationToken = default);
}
