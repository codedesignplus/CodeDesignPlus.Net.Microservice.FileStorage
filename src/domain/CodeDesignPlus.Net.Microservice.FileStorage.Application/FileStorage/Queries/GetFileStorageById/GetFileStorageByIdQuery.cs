namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageById;

public record GetFileStorageByIdQuery(Guid Id) : IRequest<FileStorageDto>;

