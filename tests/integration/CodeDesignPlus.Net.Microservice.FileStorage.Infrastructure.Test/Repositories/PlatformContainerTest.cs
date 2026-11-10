using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Mongo.Extensions;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using CodeDesignPlus.Net.Core.Abstractions.Contracts;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.Consumers;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.Jobs;
using CodeDesignPlus.Net.PubSub.Abstractions;
using Hangfire;
using MapsterMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using FileResponse = CodeDesignPlus.Net.File.Storage.Abstractions.Models.Response;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Test.Repositories;

/// <summary>
/// The files of the platform live in the container <see cref="Guid.Empty"/>, which the tenant filter of the SDK refuses:
/// against a real Mongo, uploading, releasing and cleaning one of them works, and none crosses into a tenant.
/// </summary>
[Collection(MongoContainerFixture.Collection)]
public class PlatformContainerTest
{
    private const string PlatformTarget = "platform-electronic-documents";

    private readonly IFileStorageRepository repository;

    private readonly Mock<IFileStorage> storage = new();

    private readonly Guid user = Guid.NewGuid();

    public PlatformContainerTest(MongoContainerFixture fixture)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:Enable"] = "true",
                ["Mongo:ConnectionString"] = fixture.ConnectionString,
                ["Mongo:Database"] = $"db-ms-filestorage-{Guid.NewGuid():N}",
                ["Mongo:RegisterHealthCheck"] = "false",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMongo<Startup>(configuration);

        repository = services.BuildServiceProvider().GetRequiredService<IFileStorageRepository>();

        storage.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FileResponse>());
        storage.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FileResponse>());
    }

    private Task<StoredFileDto> Store(Guid id, string target, Guid tenant)
    {
        var handler = new StoreFileCommandHandler(repository, Mock.Of<IPubSub>(), storage.Object, Mock.Of<IMapper>(), Options.Create(new FileScopeOptions { PlatformTargets = [PlatformTarget] }));

        return handler.Handle(new StoreFileCommand(id, new MemoryStream([1, 2, 3]), "SETP1.xml", target, tenant, user), CancellationToken.None);
    }

    [Fact]
    public async Task StoreFile_PlatformTarget_KeepsTheRecordInThePlatformContainer()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await Store(id, PlatformTarget, Guid.Empty);

        // Assert
        var stored = await repository.FindInContainerAsync(id, Guid.Empty, CancellationToken.None);

        Assert.Equal((Guid.Empty, PlatformTarget), (stored!.Tenant, stored.Target));
    }

    [Fact]
    public async Task StoreFile_PlatformTargetUploadedAgain_UpdatesTheSameRecord()
    {
        // Arrange
        var id = Guid.NewGuid();

        await Store(id, PlatformTarget, Guid.Empty);

        // Act
        await Store(id, PlatformTarget, Guid.Empty);

        // Assert
        Assert.NotNull(await repository.FindVisibleAsync(id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task FindInContainerAsync_FileOfATenant_IsNotFoundInAnotherContainer()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenant = Guid.NewGuid();

        await Store(id, "documents", tenant);

        // Act
        var inPlatform = await repository.FindInContainerAsync(id, Guid.Empty, CancellationToken.None);
        var inOther = await repository.FindInContainerAsync(id, Guid.NewGuid(), CancellationToken.None);
        var inOwn = await repository.FindInContainerAsync(id, tenant, CancellationToken.None);

        // Assert
        Assert.Equal((false, false, true), (inPlatform is not null, inOther is not null, inOwn is not null));
    }

    [Fact]
    public async Task ReleaseFiles_FileOfThePlatform_IsDeactivated()
    {
        // Arrange
        var id = Guid.NewGuid();

        await Store(id, PlatformTarget, Guid.Empty);

        var handler = new ReleaseFilesHandler(repository, Mock.Of<IPubSub>(), NullLogger<ReleaseFilesHandler>.Instance);

        // Act
        await handler.HandleAsync(FilesReleasedDomainEvent.Create(Guid.NewGuid(), [id], user, Guid.Empty), CancellationToken.None);

        // Assert
        Assert.Null(await repository.FindInContainerAsync(id, Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task CleanupJob_InactiveFiles_DeletesTheirRecordsOfEveryContainer()
    {
        // Arrange
        var platform = Guid.NewGuid();
        var ofTenant = Guid.NewGuid();
        var tenant = Guid.NewGuid();

        await Store(platform, PlatformTarget, Guid.Empty);
        await Store(ofTenant, "documents", tenant);

        await Deactivate(platform, Guid.Empty, Instant.FromUtc(2020, 1, 1, 0, 0));
        await Deactivate(ofTenant, tenant, Instant.FromUtc(2020, 1, 1, 0, 0));

        var job = new FileStorageCleanupJob(repository, storage.Object, NullLogger<FileStorageCleanupJob>.Instance);

        // Act
        await job.ExecuteAsync(new JobCancellationToken(false));

        // Assert
        var left = await repository.GetInactiveFilesForCleanupAsync(30, 1000, CancellationToken.None);

        Assert.DoesNotContain(left, x => x.Id == platform || x.Id == ofTenant);
    }

    /// <summary>
    /// Leaves the file inactive since a given instant, as the cleanup finds it after its retention.
    /// </summary>
    private async Task Deactivate(Guid id, Guid container, Instant since)
    {
        var aggregate = await repository.FindInContainerAsync(id, container, CancellationToken.None);

        aggregate!.Delete(user);
        aggregate.UpdatedAt = since;

        await repository.UpdateAsync(aggregate, CancellationToken.None);
    }
}
