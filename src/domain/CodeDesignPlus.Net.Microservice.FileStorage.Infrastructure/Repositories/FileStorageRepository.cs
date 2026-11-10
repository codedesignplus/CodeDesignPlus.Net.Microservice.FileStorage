namespace CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Repositories;

public class FileStorageRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<FileStorageRepository> logger)
    : RepositoryBase(serviceProvider, mongoOptions, logger), IFileStorageRepository
{
    public async Task<IEnumerable<FileStorageAggregate>> GetInactiveFilesForCleanupAsync(
        int retentionDays,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var cutoffDate = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(retentionDays));

        var collection = GetCollection<FileStorageAggregate>();

        var filter = Builders<FileStorageAggregate>.Filter.And(
            Builders<FileStorageAggregate>.Filter.Eq(x => x.IsActive, false),
            Builders<FileStorageAggregate>.Filter.Lt(x => x.UpdatedAt, cutoffDate)
        );

        return await collection
            .Find(filter)
            .Sort(Builders<FileStorageAggregate>.Sort.Ascending(x => x.UpdatedAt))
            .Limit(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileStorageAggregate?> FindVisibleAsync(Guid id, Guid sessionTenant, CancellationToken cancellationToken)
    {
        var filter = Builders<FileStorageAggregate>.Filter.And(
            Builders<FileStorageAggregate>.Filter.Eq(x => x.Id, id),
            Builders<FileStorageAggregate>.Filter.In(x => x.Tenant, new[] { sessionTenant, Guid.Empty }));

        return await GetCollection<FileStorageAggregate>().Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileStorageAggregate?> FindInContainerAsync(Guid id, Guid container, CancellationToken cancellationToken)
    {
        var filter = Builders<FileStorageAggregate>.Filter.And(InContainer(id, container), Builders<FileStorageAggregate>.Filter.Eq(x => x.IsDeleted, false));

        return await GetCollection<FileStorageAggregate>().Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteFromContainerAsync(Guid id, Guid container, CancellationToken cancellationToken)
    {
        // Sin filtrar los borrados: la limpieza se lleva precisamente los archivos ya desactivados o borrados.
        return GetCollection<FileStorageAggregate>().DeleteOneAsync(InContainer(id, container), cancellationToken);
    }

    /// <summary>
    /// El archivo de un contenedor exacto: el mismo filtro de tenant del SDK, pero aceptando el de la plataforma
    /// (<see cref="Guid.Empty"/>), que el SDK rechaza.
    /// </summary>
    private static FilterDefinition<FileStorageAggregate> InContainer(Guid id, Guid container) => Builders<FileStorageAggregate>.Filter.And(
        Builders<FileStorageAggregate>.Filter.Eq(x => x.Id, id),
        Builders<FileStorageAggregate>.Filter.Eq(x => x.Tenant, container));
}
