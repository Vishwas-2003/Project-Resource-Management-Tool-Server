using Microsoft.EntityFrameworkCore;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public abstract class CrudBaseRepository<TEntity, TKey>(AppDbContext dbContext) : ICrudBaseRepository<TEntity, TKey>
    where TEntity : class
{
    protected AppDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();

    public virtual Task<TEntity?> GetById(TKey id, CancellationToken cancellationToken = default) =>
        DbSet.FindAsync([id!], cancellationToken).AsTask();

    public virtual async Task<IReadOnlyList<TEntity>> GetAll(CancellationToken cancellationToken = default) =>
        await DbSet.ToListAsync(cancellationToken);

    public virtual Task Add(TEntity entity, CancellationToken cancellationToken = default) =>
        DbSet.AddAsync(entity, cancellationToken).AsTask();

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

    public virtual async Task<bool> Exists(TKey id, CancellationToken cancellationToken = default) =>
        await GetById(id, cancellationToken) is not null;

    public virtual Task SaveChanges(CancellationToken cancellationToken = default) =>
        DbContext.SaveChangesAsync(cancellationToken);
}
