namespace CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Repositories;

public class FileStorageRepository(IServiceProvider serviceProvider, IOptions<MongoOptions> mongoOptions, ILogger<FileStorageRepository> logger) 
    : RepositoryBase(serviceProvider, mongoOptions, logger), IFileStorageRepository
{
   
}