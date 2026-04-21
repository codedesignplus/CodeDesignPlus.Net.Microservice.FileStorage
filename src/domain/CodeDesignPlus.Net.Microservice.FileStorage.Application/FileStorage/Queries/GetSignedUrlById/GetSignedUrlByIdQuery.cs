

using CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetSignedUrlById;

public record GetSignedUrlByIdQuery(Guid Id, string File, string Target) : IRequest<FileDetail>;

