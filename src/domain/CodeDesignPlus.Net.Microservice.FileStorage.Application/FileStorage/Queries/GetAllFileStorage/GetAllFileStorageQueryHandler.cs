using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetAllFileStorage;

public class GetAllFileStorageQueryHandler(IFileStorageRepository repository, IMapper mapper) : IRequestHandler<GetAllFileStorageQuery, Pagination<FileStorageDto>>
{
    public async Task<Pagination<FileStorageDto>> Handle(GetAllFileStorageQuery request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var tenants = await repository.MatchingAsync<FileStorageAggregate>(request.Criteria, cancellationToken);

        return mapper.Map<Pagination<FileStorageDto>>(tenants);
    }
}
