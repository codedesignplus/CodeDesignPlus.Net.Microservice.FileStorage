using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageWithSignedUrls;

/// <summary>
/// Handler that retrieves a FileStorage aggregate and generates signed URLs for all files
/// </summary>
public class GetFileStorageWithSignedUrlsQueryHandler(
    IFileStorageRepository repository,
    IFileStorage fileStorage,
    IMapper mapper,
    IUserContext userContext) : IRequestHandler<GetFileStorageWithSignedUrlsQuery, FileStorageDto>
{
    public async Task<FileStorageDto> Handle(GetFileStorageWithSignedUrlsQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);
        ApplicationGuard.GuidIsEmpty(request.Id, Errors.FileNotFound);

        // Get the aggregate
        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, userContext.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileNotFound);

        // Map to DTO first
        var dto = mapper.Map<FileStorageDto>(aggregate);

        // Generate signed URLs for each file in the DTO
        var expiration = TimeSpan.FromMinutes(request.ExpirationMinutes);

        foreach (var file in dto.Files)
        {
            try
            {
                var metadata = file.FileDetail.Metadata;

                // Generate signed URL using the provider
                var signedUrlResponse = await fileStorage.GetSignedUrlAsync(
                    metadata.File,
                    metadata.Target,
                    expiration,
                    cancellationToken);

                // Update metadata with signed URL information
                // Metadata has public setters for SignedUrl and SignedUrlExpiration
                metadata.SignedUrl = signedUrlResponse.File.Detail.SignedUrl;
                metadata.SignedUrlExpiration = signedUrlResponse.File.Detail.SignedUrlExpiration;
            }
            catch (Exception)
            {
                // Log error but continue with other files
                // The file will simply not have a signed URL (will remain null)
            }
        }

        return dto;
    }
}
