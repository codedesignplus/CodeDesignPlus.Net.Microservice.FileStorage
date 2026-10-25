using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.ImportFile;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using M = CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.ImportFile;

/// <summary>
/// La importación de un archivo que otro sistema dejó en un contenedor de la cuenta (una grabación): solo de los
/// contenedores declarados, copiado a <c>{target}/{id}</c> con su registro, y el origen se borra después de registrarlo.
/// </summary>
public class ImportFileCommandHandlerTest
{
    private const string Source = "acs-recordings";
    private const string SourcePath = "20261021/call/recording/0-mixed.mp4";
    private const string Target = "assembly-recordings";
    private const string Name = "session-1-part-1.mp4";

    private readonly Mock<IFileStorageRepository> repository = new();
    private readonly Mock<IPubSub> pubsub = new();
    private readonly Mock<IFileStorage> fileStorage = new();
    private readonly Mock<IMapper> mapper = new();

    private readonly Guid id = Guid.NewGuid();
    private readonly Guid tenant = Guid.NewGuid();
    private readonly Guid uploadedBy = Guid.NewGuid();

    private readonly FileScopeOptions scope = new() { PlatformTargets = ["users"], ImportSources = [Source] };

    private readonly List<string> calls = [];

    public ImportFileCommandHandlerTest()
    {
        fileStorage
            .Setup(x => x.DeleteImportSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("release"))
            .ReturnsAsync([]);

        repository
            .Setup(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("create"))
            .Returns(Task.CompletedTask);
    }

    private ImportFileCommandHandler Handler() => new(repository.Object, pubsub.Object, fileStorage.Object, mapper.Object, Options.Create(scope), NullLogger<ImportFileCommandHandler>.Instance);

    private ImportFileCommand Command(string source = Source, string path = SourcePath) => new(id, source, path, Name, Target, tenant, uploadedBy);

    /// <summary>
    /// Un contenedor que el despliegue no declara —el de otra copropiedad, por ejemplo— no se lee nunca.
    /// </summary>
    [Theory]
    [InlineData("other-container")]
    [InlineData("2b1f3f5e-0000-0000-0000-000000000001")]
    public async Task Handle_SourceNotDeclared_ThrowsImportSourceNotAllowed(string source)
    {
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(Command(source: source), CancellationToken.None));

        Assert.Equal(Errors.ImportSourceNotAllowed.GetCode(), exception.Code);
        fileStorage.Verify(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Una ruta que sale del contenedor declarado tampoco se lee.
    /// </summary>
    [Theory]
    [InlineData("../2b1f3f5e-0000-0000-0000-000000000001/minutes.pdf")]
    [InlineData("/absolute.mp4")]
    [InlineData(@"a\..\b.mp4")]
    [InlineData("a//b.mp4")]
    public async Task Handle_PathOutsideTheSource_ThrowsImportSourceNotAllowed(string path)
    {
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(Command(path: path), CancellationToken.None));

        Assert.Equal(Errors.ImportSourceNotAllowed.GetCode(), exception.Code);
        fileStorage.Verify(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Un archivo de un tipo que el registro no acepta no se copia: quedaba el blob copiado y sin registro (Kappali
    /// pendings/526).
    /// </summary>
    [Theory]
    [InlineData("recording")]
    [InlineData("recording.acsraw")]
    public async Task Handle_FileTypeTheRecordRejects_ThrowsBeforeCopying(string fileName)
    {
        var command = new ImportFileCommand(id, Source, SourcePath, fileName, Target, tenant, uploadedBy);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(command, CancellationToken.None));

        Assert.Equal(Errors.FileTypeNotAccepted.GetCode(), exception.Code);
        fileStorage.Verify(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// La copia queda en <c>{target}/{id}/{nombre}</c> de la copropiedad, nace el registro a nombre de quien importa y
    /// solo después se borra el origen.
    /// </summary>
    [Fact]
    public async Task Handle_NewImport_CopiesRegistersAndThenReleasesTheSource()
    {
        var (response, file) = Imported();
        fileStorage
            .Setup(x => x.ImportAsync(Source, SourcePath, Name, $"{Target}/{id}", tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync([response]);
        mapper.Setup(x => x.Map<Domain.ValueObjects.File>(response)).Returns(file);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        repository.Verify(x => x.CreateAsync(It.Is<FileStorageAggregate>(a =>
            a.Id == id &&
            a.Tenant == tenant &&
            a.Target == Target &&
            a.File == Name &&
            a.CreatedBy == uploadedBy &&
            a.Files.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.DeleteImportSourceAsync(Source, SourcePath, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(["create", "release"], calls);
        Assert.Equal(id, result.Id);
        Assert.Equal(Target, result.Target);
        Assert.Equal(Name, result.FileName);
    }

    /// <summary>
    /// Si la copia falla no hay registro y el origen se queda, para que el siguiente intento vuelva a copiar. El error lleva
    /// la causa que dio el almacenamiento: sin ella, quien importa solo sabía que «no se pudo copiar».
    /// </summary>
    [Fact]
    public async Task Handle_CopyFails_ThrowsImportFailedWithTheCauseAndKeepsTheSource()
    {
        fileStorage
            .Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new M.Response(new M.File(Name), TypeProviders.AzureBlobProvider) { Success = false, Message = "NullReferenceException: Object reference not set to an instance of an object." }]);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(Command(), CancellationToken.None));

        Assert.Equal(Errors.ImportFailed.GetCode(), exception.Code);
        Assert.Contains("AzureBlobProvider: NullReferenceException: Object reference not set to an instance of an object.", exception.Message);
        repository.Verify(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        fileStorage.Verify(x => x.DeleteImportSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Sin ningún almacenamiento activo nada se copió: no se registra un archivo que no existe.
    /// </summary>
    [Fact]
    public async Task Handle_NoProviderAnswers_ThrowsImportFailed()
    {
        fileStorage
            .Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => Handler().Handle(Command(), CancellationToken.None));

        Assert.Equal(Errors.ImportFailed.GetCode(), exception.Code);
        repository.Verify(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Con el mismo id ya registrado (un aviso repetido, o el borrado del origen falló la vez anterior) no se copia ni se
    /// registra otra vez: solo se vuelve a soltar el origen.
    /// </summary>
    [Fact]
    public async Task Handle_AlreadyImported_OnlyReleasesTheSource()
    {
        var existing = FileStorageAggregate.Create(id, Name, Target, tenant, uploadedBy);
        existing.AddFile(Imported().File, uploadedBy);
        existing.GetAndClearEvents();
        repository.Setup(x => x.FindAsync<FileStorageAggregate>(id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await Handler().Handle(Command(), CancellationToken.None);

        fileStorage.Verify(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.CreateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.UpdateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        fileStorage.Verify(x => x.DeleteImportSourceAsync(Source, SourcePath, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(id, result.Id);
    }

    private (M.Response Response, Domain.ValueObjects.File File) Imported()
    {
        var model = new M.File(Name);
        var response = new M.Response(model, TypeProviders.AzureBlobProvider) { Success = true };
        var metadata = Metadata.Create(Name, $"{Target}/{id}", new Uri("http://example.com"), "http://example.com/d", "http://example.com/v", TypeProviders.AzureBlobProvider.ToString());
        var detail = FileDetail.Create(model.Extension, model.FullName, model.Name, metadata, 2048, model.Version.ToString(), false, model.Mime);

        return (response, new Domain.ValueObjects.File(true, string.Empty, detail, TypeProviders.AzureBlobProvider.ToString()));
    }
}
