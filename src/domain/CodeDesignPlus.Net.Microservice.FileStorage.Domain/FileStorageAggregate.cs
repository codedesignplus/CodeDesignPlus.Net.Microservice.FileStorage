namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain;

public class FileStorageAggregate(Guid id) : AggregateRoot(id)
{
    public string File { get; private set; } = null!;
    public string Target { get; private set; } = null!;
    public List<ValueObjects.File> Files { get; private set; } = [];

    public static FileStorageAggregate Create(Guid id, string file, string target, Guid tenant, Guid createdBy)
    {
        var aggregate = new FileStorageAggregate(id)
        {
            Tenant = tenant,
            CreatedBy = createdBy,
            File = file,
            Target = target,
            IsActive = true,
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        aggregate.AddEvent(FileStorageCreatedDomainEvent.Create(id, file, target, tenant, createdBy));

        return aggregate;
    }

    public void AddFile(ValueObjects.File file, Guid updatedBy)
    {
        DomainGuard.IsNull(file, Errors.FileDetailIsInvalid);
        DomainGuard.GuidIsEmpty(updatedBy, Errors.UpdateByIsInvalid);

        var exist = Files.Any(x => x.FileDetail.FullName == file.FileDetail.FullName && x.Provider == file.Provider);

        DomainGuard.IsTrue(exist, Errors.FileAlreadyExists);

        Files.Add(file);
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        this.AddEvent(FileStorageAddedDomainEvent.Create(Id, file, Tenant));
    }

    public void Delete(Guid updatedBy)
    {
        DomainGuard.GuidIsEmpty(updatedBy, Errors.UpdateByIsInvalid);

        IsActive = false;
        IsDeleted = true;
        UpdatedBy = updatedBy;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        this.AddEvent(FileStorageDeletedDomainEvent.Create(Id, File, Target, Tenant, updatedBy));
    }
}
