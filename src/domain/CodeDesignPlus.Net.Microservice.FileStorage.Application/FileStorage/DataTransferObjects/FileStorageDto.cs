namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

public class FileStorageDto: IDtoBase
{
    public required Guid Id { get; set; }
    public List<Domain.ValueObjects.File> Files { get; set; } = [];
}