using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.StoreFile;

/// <summary>
/// La subida con copropiedad y usuario explícitos, que es la que usan el REST y el gRPC (pendings/260, regla 56): solo
/// targets permitidos, los de plataforma en el contenedor de la plataforma y cada archivo en <c>{target}/{id}</c> con su
/// registro.
/// </summary>
public class StoreFileCommandHandlerTest
{
    private readonly Mock<IFileStorageRepository> repository = new();
    private readonly Mock<IPubSub> pubsub = new();
    private readonly Mock<IFileStorage> fileStorage = new();
    private readonly Mock<IMapper> mapper = new();

    private readonly Guid tenant = Guid.NewGuid();
    private readonly Guid uploadedBy = Guid.NewGuid();

    private readonly FileScopeOptions scope = new() { PlatformTargets = ["users", "system-email-templates"] };

    private StoreFileCommandHandler Handler() => new(repository.Object, pubsub.Object, fileStorage.Object, mapper.Object, Options.Create(scope));

    /// <summary>
    /// A folder whose name could leave its place, or is not a plain name, never reaches the blob nor creates a record
    /// (pendings/302).
    /// </summary>
    [Theory]
    [InlineData("../other")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData(@"a\b")]
    [InlineData("Cash-Deposits")]
    [InlineData("-leading-hyphen")]
    [InlineData("with space")]
    [InlineData("")]
    [InlineData("a1234567890123456789012345678901234567890123456789012345678901234")]
    public async Task Handle_UnsafeTarget_ThrowsInvalidTarget(string target)
    {
        var command = new StoreFileCommand(Guid.NewGuid(), new MemoryStream([1, 2]), "photo.jpg", target, tenant, uploadedBy);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(command, CancellationToken.None));

        Assert.Equal(Errors.InvalidTarget.GetCode(), exception.Code);
        fileStorage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Any safe name is accepted without the service knowing it beforehand, and a folder that is not a platform one is
    /// stored in the condominium in session (pendings/302).
    /// </summary>
    [Theory]
    [InlineData("cash-deposits")]
    [InlineData("a")]
    [InlineData("folder-2026")]
    [InlineData("a123456789012345678901234567890123456789012345678901234567890123")]
    public async Task Handle_SafeTarget_StoresInSessionTenant(string target)
    {
        var id = Guid.NewGuid();
        fileStorage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await Handler().Handle(new StoreFileCommand(id, new MemoryStream([1, 2]), "support.png", target, tenant, uploadedBy), CancellationToken.None);

        fileStorage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "support.png", $"{target}/{id}", false, tenant, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.CreateAsync(It.Is<FileStorageAggregate>(a => a.Tenant == tenant && a.Target == target), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A platform folder is whatever the configuration says, not a fixed list (pendings/302).</summary>
    [Fact]
    public async Task Handle_ConfiguredPlatformTarget_StoresInPlatformContainer()
    {
        scope.PlatformTargets = ["shared-assets"];
        var id = Guid.NewGuid();
        fileStorage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await Handler().Handle(new StoreFileCommand(id, new MemoryStream([1, 2]), "logo.png", "shared-assets", tenant, uploadedBy), CancellationToken.None);
        await Handler().Handle(new StoreFileCommand(Guid.NewGuid(), new MemoryStream([1, 2]), "me.png", "users", tenant, uploadedBy), CancellationToken.None);

        fileStorage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "logo.png", $"shared-assets/{id}", false, Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), "me.png", It.IsAny<string>(), false, tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Sin petición no hay nada que guardar.</summary>
    [Fact]
    public async Task Handle_RequestIsNull_ThrowsInvalidRequest()
    {
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(null!, CancellationToken.None));

        Assert.Equal(Errors.InvalidRequest.GetCode(), exception.Code);
    }

    /// <summary>
    /// Un target de plataforma va al contenedor de la plataforma aunque la petición traiga una copropiedad: el archivo
    /// no es de ninguna.
    /// </summary>
    [Theory]
    [InlineData("users", "me.png")]
    [InlineData("system-email-templates", "terms.pdf")]
    public async Task Handle_PlatformTarget_StoresInPlatformContainer(string target, string name)
    {
        var id = Guid.NewGuid();
        fileStorage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await Handler().Handle(new StoreFileCommand(id, new MemoryStream([1, 2]), name, target, tenant, uploadedBy), CancellationToken.None);

        repository.Verify(x => x.FindAsync<FileStorageAggregate>(id, Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.UploadAsync(It.IsAny<Stream>(), name, $"{target}/{id}", false, Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.CreateAsync(It.Is<FileStorageAggregate>(a => a.Tenant == Guid.Empty), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// El registro nace a nombre de quien pidió la operación —no de una sesión, que un job no tiene—, en la copropiedad
    /// indicada, y el blob queda en <c>{target}/{id}</c> con el nombre original. La respuesta trae el nombre guardado.
    /// </summary>
    [Fact]
    public async Task Handle_NewFile_CreatesRecordWithUploaderUnderIdFolder()
    {
        var id = Guid.NewGuid();
        var stream = new MemoryStream([1, 2]);
        var (response, file) = Uploaded("soat.pdf", $"vehicle-documents/{id}");
        fileStorage.Setup(x => x.UploadAsync(stream, "soat.pdf", $"vehicle-documents/{id}", false, tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync([response]);
        mapper.Setup(x => x.Map<Domain.ValueObjects.File>(response)).Returns(file);

        var result = await Handler().Handle(new StoreFileCommand(id, stream, "soat.pdf", "vehicle-documents", tenant, uploadedBy), CancellationToken.None);

        repository.Verify(x => x.CreateAsync(It.Is<FileStorageAggregate>(a =>
            a.Id == id &&
            a.Tenant == tenant &&
            a.Target == "vehicle-documents" &&
            a.File == "soat.pdf" &&
            a.CreatedBy == uploadedBy &&
            a.UpdatedBy == uploadedBy &&
            a.Files.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.UpdateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        pubsub.Verify(x => x.PublishAsync(It.Is<IReadOnlyList<IDomainEvent>>(e => e.Count == 2), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(id, result.Id);
        Assert.Equal("vehicle-documents", result.Target);
        Assert.Equal(file.FileDetail.FullName, result.FileName);
    }

    /// <summary>Si el registro ya existe se le añade el archivo y se actualiza, no se crea otro.</summary>
    [Fact]
    public async Task Handle_ExistingRecord_AddsFileAndUpdates()
    {
        var id = Guid.NewGuid();
        var existing = FileStorageAggregate.Create(id, "license.pdf", "licenses-pdf", tenant, Guid.NewGuid());
        existing.GetAndClearEvents();
        repository.Setup(x => x.FindAsync<FileStorageAggregate>(id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var (response, file) = Uploaded("license.pdf", $"licenses-pdf/{id}");
        fileStorage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([response]);
        mapper.Setup(x => x.Map<Domain.ValueObjects.File>(response)).Returns(file);

        await Handler().Handle(new StoreFileCommand(id, new MemoryStream([1, 2]), "license.pdf", "licenses-pdf", tenant, uploadedBy), CancellationToken.None);

        repository.Verify(x => x.UpdateAsync(It.Is<FileStorageAggregate>(a => a.Files.Count == 1 && a.UpdatedBy == uploadedBy), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (File.Storage.Abstractions.Models.Response Response, Domain.ValueObjects.File File) Uploaded(string name, string folder)
    {
        var model = new File.Storage.Abstractions.Models.File(name);
        var response = new File.Storage.Abstractions.Models.Response(model, TypeProviders.AzureBlobProvider);
        var metadata = Metadata.Create(name, folder, new Uri("http://example.com"), "http://example.com/d", "http://example.com/v", TypeProviders.AzureBlobProvider.ToString());
        var detail = FileDetail.Create(model.Extension, model.FullName, model.Name, metadata, 2, model.Version.ToString(), false, model.Mime);

        return (response, new Domain.ValueObjects.File(true, string.Empty, detail, TypeProviders.AzureBlobProvider.ToString()));
    }
}
