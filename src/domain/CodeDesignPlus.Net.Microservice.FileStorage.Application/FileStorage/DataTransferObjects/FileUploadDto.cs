using System;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

public class FileUploadDto : IDtoBase
{
    public Guid Id { get; set; }
    public string Target { get; set; } = null!;
    public bool Renowned { get; set; }
}
