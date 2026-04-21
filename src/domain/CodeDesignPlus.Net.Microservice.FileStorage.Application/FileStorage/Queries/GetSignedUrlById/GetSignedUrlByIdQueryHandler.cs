using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetSignedUrlById;

public class GetSignedUrlByIdQueryHandler(IFileStorageRepository repository, IFileStorage fileStorage) : IRequestHandler<GetSignedUrlByIdQuery, FileDetail>
{
    public async Task<FileDetail> Handle(GetSignedUrlByIdQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var exist = await repository.ExistsAsync<FileStorageAggregate>(request.Id, cancellationToken);

        ApplicationGuard.IsFalse(exist, Errors.FileNotFound);

        var response = await fileStorage.GetSignedUrlAsync(request.File, request.Target, TimeSpan.FromMinutes(5), cancellationToken);

        return response.File.Detail;
    }
}
