
namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.DomainEvents;

[EventKey<FileStorageAggregate>(1, "FileStorageAddedDomainEvent")]
public class FileStorageAddedDomainEvent(
     Guid aggregateId,
     ValueObjects.File file,
     Guid tenant,
     Guid? eventId = null,
     Instant? occurredAt = null,
     Dictionary<string, object>? metadata = null
) : DomainEvent(aggregateId, eventId, occurredAt, metadata)
{
    public ValueObjects.File File { get; } = file;
    public Guid Tenant { get; } = tenant;

    public static FileStorageAddedDomainEvent Create(Guid aggregateId, ValueObjects.File file, Guid tenant)
    {
        return new FileStorageAddedDomainEvent(aggregateId, file, tenant);
    }
}
