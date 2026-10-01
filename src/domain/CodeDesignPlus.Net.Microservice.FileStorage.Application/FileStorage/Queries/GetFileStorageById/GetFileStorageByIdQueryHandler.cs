namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageById;

public class GetFileStorageByIdQueryHandler(IFileStorageRepository repository, IMapper mapper, ICacheManager cacheManager, IUserContext user) : IRequestHandler<GetFileStorageByIdQuery, FileStorageDto>
{
    public async Task<FileStorageDto> Handle(GetFileStorageByIdQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        // La clave lleva la copropiedad: con solo el id, lo que una copropiedad dejaba en caché lo leía otra (pendings/168).
        var key = $"{user.Tenant}:{request.Id}";

        if (await cacheManager.ExistsAsync(key))
            return await cacheManager.GetAsync<FileStorageDto>(key);

        var aggregate = await repository.FindVisibleAsync(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileStorageDoesNotExists);

        var dto = mapper.Map<FileStorageDto>(aggregate);

        await cacheManager.SetAsync(key, dto);

        return dto;
    }
}
