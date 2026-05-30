namespace Prm.Api.Services.Interfaces;

public interface ICrudBaseService<TDto, TKey, TCreateRequest, TUpdateRequest>
{
    Task<TDto> Get(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TDto>> GetAll(CancellationToken cancellationToken = default);
    Task<TKey> Add(TCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(TKey id, TUpdateRequest request, CancellationToken cancellationToken = default);
    Task Delete(TKey id, CancellationToken cancellationToken = default);
}
