namespace Prm.Data.Audit;

public sealed class NullCurrentUserService : ICurrentUserService
{
    public static NullCurrentUserService Instance { get; } = new();

    public int? GetUserId() => null;
}
