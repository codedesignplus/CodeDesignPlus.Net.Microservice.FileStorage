namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;

public record DownloadQuery(Guid Id, string File, string Target) : IRequest<File.Storage.Abstractions.Models.Response>;

