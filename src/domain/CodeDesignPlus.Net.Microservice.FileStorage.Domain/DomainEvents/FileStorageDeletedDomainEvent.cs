namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.DomainEvents;

[EventKey<FileStorageAggregate>(1, "FileStorageDeletedDomainEvent", autoCreate: false)]
public class FileStorageDeletedDomainEvent(
     Guid aggregateId,
    string file,
    string target,
     Guid tenant,
     Guid? updatedBy,
     Guid? eventId = null,
     Instant? occurredAt = null,
     Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public string File { get; } = file;
    public string Target { get; } = target;
    public Guid Tenant { get; } = tenant;
    public Guid? UpdatedBy { get; } = updatedBy;

    public static FileStorageDeletedDomainEvent Create(Guid aggregateId, string file, string target, Guid tenant, Guid? updatedBy)
    {
        return new FileStorageDeletedDomainEvent(aggregateId, file, target, tenant, updatedBy);
    }
}
