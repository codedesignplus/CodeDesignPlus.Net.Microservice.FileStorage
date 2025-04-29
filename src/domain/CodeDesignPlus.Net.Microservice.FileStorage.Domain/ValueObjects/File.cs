using System.Text.Json.Serialization;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;

public sealed partial class File
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = null!;
    public FileDetail FileDetail { get; private set; } = null!;
    public string Provider { get; private set; } = null!;

    public File()
    {

    }

    [JsonConstructor]
    public File(bool success, string message, FileDetail fileDetail, string provider)
    {
        DomainGuard.IsNull(fileDetail, Errors.FileDetailIsInvalid);
        DomainGuard.IsNullOrEmpty(provider, Errors.ProviderIsInvalid);

        Success = success;
        Message = message;
        FileDetail = fileDetail;
        Provider = provider;
    }

    public static File Create(bool success, string message, FileDetail fileDetail, string provider)
    {
        return new File(success, message, fileDetail, provider);
    }
}
