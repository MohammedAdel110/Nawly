using Nawly.Domain.Contracts;
using Nawly.Domain.Events;

namespace Nawly.Domain.Models.Common;

public abstract class BaseEntity<TKey> : IEntity<TKey>, IAuditable, ISoftDeletable
{
    public TKey Id { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// Base entity using time-ordered UUID v7 keys generated in application code (SRS §6.1).
/// </summary>
public abstract class BaseGuidEntity : BaseEntity<Guid>
{
    protected BaseGuidEntity()
    {
        Id = Guid.CreateVersion7();
    }
}

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TKey> : BaseEntity<TKey>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot()
    {
        Id = Guid.CreateVersion7();
    }
}
