namespace Nawly.Domain.Exceptions.NotFoundHandler;

public sealed class EntityNotFoundException<TEntity> : NotFoundCustomException
{
    public EntityNotFoundException(object key)
        : base($"{typeof(TEntity).Name} with key '{key}' was not found.", $"{typeof(TEntity).Name.ToLowerInvariant()}.not_found")
    {
    }
}
