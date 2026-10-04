using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeactivateFileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Moq;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage;

/// <summary>
/// Where each file lives and who can reach it (pendings/167 and 168): one folder per id, platform files outside any
/// condominium, and every read or write of an existing file goes through the stored blob of a visible record.
/// </summary>
/// <remarks>
/// Las pruebas de la subida viven en <c>StoreFileCommandHandlerTest</c>: el handler REST ya no guarda, delega (pendings/260).
/// </remarks>
public class FileScopeHandlersTest
{
    private readonly Mock<IFileStorageRepository> repository = new();
    private readonly Mock<IUserContext> user = new();
    private readonly Mock<IPubSub> pubsub = new();
    private readonly Mock<IFileStorage> fileStorage = new();

    private readonly Guid tenant = Guid.NewGuid();
    private readonly Guid userId = Guid.NewGuid();

    public FileScopeHandlersTest()
    {
        user.SetupGet(x => x.Tenant).Returns(tenant);
        user.SetupGet(x => x.IdUser).Returns(userId);
    }

    [Fact]
    public async Task Deactivate_FileOfAnotherCondominium_ThrowsDoesNotExist()
    {
        var handler = new DeactivateFileStorageCommandHandler(repository.Object, user.Object, pubsub.Object);
        repository.Setup(x => x.FindVisibleAsync(It.IsAny<Guid>(), tenant, It.IsAny<CancellationToken>())).ReturnsAsync((FileStorageAggregate?)null);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(new DeactivateFileStorageCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(Errors.FileStorageDoesNotExists.GetCode(), exception.Code);
        repository.Verify(x => x.UpdateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_PlatformFileOfAnotherUser_ThrowsDoesNotExist()
    {
        var handler = new DeactivateFileStorageCommandHandler(repository.Object, user.Object, pubsub.Object);
        var aggregate = FileStorageAggregate.Create(Guid.NewGuid(), "me.png", "users", Guid.Empty, Guid.NewGuid());
        repository.Setup(x => x.FindVisibleAsync(aggregate.Id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(aggregate);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(new DeactivateFileStorageCommand(aggregate.Id), CancellationToken.None));

        Assert.Equal(Errors.FileStorageDoesNotExists.GetCode(), exception.Code);
        Assert.True(aggregate.IsActive);
    }

    [Fact]
    public async Task Deactivate_PlatformFileOfSameUser_Deactivates()
    {
        var handler = new DeactivateFileStorageCommandHandler(repository.Object, user.Object, pubsub.Object);
        var aggregate = FileStorageAggregate.Create(Guid.NewGuid(), "me.png", "users", Guid.Empty, userId);
        repository.Setup(x => x.FindVisibleAsync(aggregate.Id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(aggregate);

        await handler.Handle(new DeactivateFileStorageCommand(aggregate.Id), CancellationToken.None);

        repository.Verify(x => x.UpdateAsync(It.Is<FileStorageAggregate>(a => !a.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Download_UsesStoredBlobOfTheRecord()
    {
        var handler = new DownloadQueryHandler(repository.Object, user.Object, fileStorage.Object);
        var aggregate = WithStoredFile("photo.jpg", "common-areas");
        repository.Setup(x => x.FindVisibleAsync(aggregate.Id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(aggregate);
        var download = new File.Storage.Abstractions.Models.Response(new File.Storage.Abstractions.Models.File("photo.jpg"), TypeProviders.AzureBlobProvider) { Stream = new MemoryStream([1]) };
        fileStorage.Setup(x => x.DownloadAsync("photo.jpg", $"common-areas/{aggregate.Id}", tenant, It.IsAny<CancellationToken>())).ReturnsAsync(download);

        var result = await handler.Handle(new DownloadQuery(aggregate.Id), CancellationToken.None);

        Assert.Same(download, result);
    }

    [Fact]
    public async Task Download_InactiveFile_ThrowsFileNotFound()
    {
        var handler = new DownloadQueryHandler(repository.Object, user.Object, fileStorage.Object);
        var aggregate = WithStoredFile("photo.jpg", "common-areas");
        aggregate.Delete(userId);
        repository.Setup(x => x.FindVisibleAsync(aggregate.Id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(aggregate);
        // The blob still exists for 30 days: only the inactive check may stop the download.
        var download = new File.Storage.Abstractions.Models.Response(new File.Storage.Abstractions.Models.File("photo.jpg"), TypeProviders.AzureBlobProvider) { Stream = new MemoryStream([1]) };
        fileStorage.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(download);

        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(new DownloadQuery(aggregate.Id), CancellationToken.None));

        Assert.Equal(Errors.FileNotFound.GetCode(), exception.Code);
    }

    [Fact]
    public async Task Delete_RemovesTheStoredBlobNotTheOriginalName()
    {
        var handler = new DeleteFileStorageCommandHandler(repository.Object, fileStorage.Object, user.Object, pubsub.Object);
        var aggregate = WithStoredFile("photo.jpg", "common-areas");
        repository.Setup(x => x.FindVisibleAsync(aggregate.Id, tenant, It.IsAny<CancellationToken>())).ReturnsAsync(aggregate);

        await handler.Handle(new DeleteFileStorageCommand(aggregate.Id), CancellationToken.None);

        fileStorage.Verify(x => x.DeleteAsync("photo.jpg", $"common-areas/{aggregate.Id}", tenant, It.IsAny<CancellationToken>()), Times.Once);
        fileStorage.Verify(x => x.DeleteAsync(It.IsAny<string>(), "common-areas", It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private FileStorageAggregate WithStoredFile(string name, string target)
    {
        var aggregate = FileStorageAggregate.Create(Guid.NewGuid(), name, target, tenant, userId);
        var folder = FileScope.FolderFor(target, aggregate.Id);
        var model = new File.Storage.Abstractions.Models.File(name);
        var metadata = Metadata.Create(name, folder, new Uri("http://example.com"), "http://example.com/d", "http://example.com/v", TypeProviders.AzureBlobProvider.ToString());
        var detail = FileDetail.Create(model.Extension, model.FullName, model.Name, metadata, 2, model.Version.ToString(), false, model.Mime);
        aggregate.AddFile(new Domain.ValueObjects.File(true, string.Empty, detail, TypeProviders.AzureBlobProvider.ToString()), userId);
        aggregate.GetAndClearEvents();
        return aggregate;
    }
}
