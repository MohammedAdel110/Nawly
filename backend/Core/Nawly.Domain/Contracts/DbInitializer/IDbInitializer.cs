namespace Nawly.Domain.Contracts.DbInitializer;

public interface IDbInitializer
{
    Task DataSeedAsync(CancellationToken cancellationToken = default);
}
