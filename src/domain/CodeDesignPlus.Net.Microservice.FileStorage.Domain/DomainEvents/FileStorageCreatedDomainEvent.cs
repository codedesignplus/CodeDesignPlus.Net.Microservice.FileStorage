
namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.DomainEvents;

[EventKey<FileStorageAggregate>(1, "FileStorageCreatedDomainEvent")]
public class FileStorageCreatedDomainEvent(
    Guid aggregateId,
    string file,
    string target,
    Guid tenant,
    Guid createdBy,
    Guid? eventId = null,
    Instant? occurredAt = null,
    Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public string File { get; } = file;
    public string Target { get; } = target;
    public Guid Tenant { get; } = tenant;
    public Guid CreatedBy { get; } = createdBy;

    public static FileStorageCreatedDomainEvent Create(Guid aggregateId, string file, string target, Guid tenant, Guid createdBy)
    {
        return new FileStorageCreatedDomainEvent(aggregateId, file, target, tenant, createdBy);
    }
}
