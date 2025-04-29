using System.Text.Json.Serialization;
using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;

public sealed partial class FileDetail
{
    public string Extension { get; private set; } = null!;
    public string FullName { get;private set;  } = null!;
    public string Name { get; private set; } = null!;
    public Metadata Metadata { get; private set; } = null!;
    public long Size { get;private set;  } = 0;
    public string Version { get; private set; } = null!;
    public bool Renowned { get; private set; } = false;
    public ApacheMime Mime { get; private set; } = null!;

    public FileDetail()
    {
    }

    [JsonConstructor]
    public FileDetail(string extension, string fullName, string name, Metadata metadata, long size, string version, bool renowned, ApacheMime mime)
    {
        DomainGuard.IsNullOrEmpty(extension, Errors.ExtensionIsInvalid);
        DomainGuard.IsNullOrEmpty(fullName, Errors.FullNameIsInvalid);
        DomainGuard.IsNullOrEmpty(name, Errors.FullNameIsInvalid);
        DomainGuard.IsNull(metadata, Errors.MetadataIsInvalid);
        DomainGuard.IsTrue(size <= 1, Errors.FileSizeIsInvalid);
        DomainGuard.IsNullOrEmpty(version, Errors.VersionIsInvalid);
        DomainGuard.IsNull(mime, Errors.FileIsInvalid);

        this.Extension = extension;
        this.FullName = fullName;
        this.Name = name;
        this.Metadata = metadata;
        this.Size = size;
        this.Version = version;
        this.Renowned = renowned;
        this.Mime = mime;
    }
    public static FileDetail Create(string extension, string fullName, string name, Metadata detail, long size, string version, bool renowned, ApacheMime mime)
    {
        return new FileDetail(extension, fullName, name, detail, size, version, renowned, mime);
    }
}
