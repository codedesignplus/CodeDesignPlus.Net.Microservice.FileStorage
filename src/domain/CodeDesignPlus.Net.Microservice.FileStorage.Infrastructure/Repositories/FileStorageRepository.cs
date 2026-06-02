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
}