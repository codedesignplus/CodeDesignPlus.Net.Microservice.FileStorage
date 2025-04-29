using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetAllFileStorage;

public record GetAllFileStorageQuery(C.Criteria Criteria) : IRequest<Pagination<FileStorageDto>>;

