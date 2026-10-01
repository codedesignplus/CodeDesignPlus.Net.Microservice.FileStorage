namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;

public record DownloadQuery(Guid Id) : IRequest<File.Storage.Abstractions.Models.Response>;

