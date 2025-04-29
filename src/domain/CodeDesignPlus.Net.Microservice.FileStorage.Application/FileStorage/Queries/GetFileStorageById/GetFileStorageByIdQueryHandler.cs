namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageById;

public class GetFileStorageByIdQueryHandler(IFileStorageRepository repository, IMapper mapper, ICacheManager cacheManager, IUserContext user) : IRequestHandler<GetFileStorageByIdQuery, FileStorageDto>
{
    public async Task<FileStorageDto> Handle(GetFileStorageByIdQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var exists = await cacheManager.ExistsAsync(request.Id.ToString());

        if (exists)
            return await cacheManager.GetAsync<FileStorageDto>(request.Id.ToString());

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileStorageDoesNotExists);

        var dto = mapper.Map<FileStorageDto>(aggregate);

        await cacheManager.SetAsync(request.Id.ToString(), dto);

        return dto;
    }
}
