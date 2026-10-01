using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetSignedUrlById;

public class GetSignedUrlByIdQueryHandler(IFileStorageRepository repository, IFileStorage fileStorage, IUserContext user) : IRequestHandler<GetSignedUrlByIdQuery, FileDetail>
{
    public async Task<FileDetail> Handle(GetSignedUrlByIdQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var aggregate = await repository.FindVisibleAsync(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileNotFound);
        ApplicationGuard.IsFalse(aggregate!.IsActive, Errors.FileNotFound);

        var stored = aggregate.Files.FirstOrDefault(x => x.Success);

        ApplicationGuard.IsNull(stored, Errors.FileNotFound);

        var (name, folder) = FileScope.BlobOf(stored!, aggregate.Target);

        var response = await fileStorage.GetSignedUrlAsync(name, folder, TimeSpan.FromMinutes(5), aggregate.Tenant, cancellationToken);

        return response.File.Detail;
    }
}
