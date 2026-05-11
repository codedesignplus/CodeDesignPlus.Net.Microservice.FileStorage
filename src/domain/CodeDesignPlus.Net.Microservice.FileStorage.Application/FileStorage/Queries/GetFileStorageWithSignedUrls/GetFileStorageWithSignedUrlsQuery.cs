using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageWithSignedUrls;

/// <summary>
/// Query to get a FileStorage aggregate with signed URLs for all files
/// </summary>
/// <param name="Id">The aggregate ID</param>
/// <param name="ExpirationMinutes">Optional expiration time in minutes (default: 5)</param>
public record GetFileStorageWithSignedUrlsQuery(Guid Id, int ExpirationMinutes = 5) : IRequest<FileStorageDto>;
