using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;

public class DownloadQueryHandler(IFileStorageRepository repository, IUserContext user, IFileStorage fileStorage) : IRequestHandler<DownloadQuery, File.Storage.Abstractions.Models.Response>
{
    public async Task<File.Storage.Abstractions.Models.Response> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        var exist = await repository.ExistsAsync<FileStorageAggregate>(request.Id, user.Tenant, cancellationToken);

        DomainGuard.IsFalse(exist, Errors.FileNotFound);

        var file = await fileStorage.DownloadAsync(request.File, request.Target, cancellationToken);

        return file;
    }
}
