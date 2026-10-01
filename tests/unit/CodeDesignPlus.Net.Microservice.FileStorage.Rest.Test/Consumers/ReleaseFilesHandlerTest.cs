using System.Collections.Generic;
using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.Core.Abstractions.Contracts;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.Consumers;
using CodeDesignPlus.Net.PubSub.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test.Consumers;

/// <summary>
/// Un micro que suelta archivos los deja desactivados, solo en su copropiedad y sin fallar si el evento se repite
/// (pendings/172).
/// </summary>
public class ReleaseFilesHandlerTest
{
    private readonly Mock<IFileStorageRepository> repository = new();
    private readonly Mock<IPubSub> pubsub = new();
    private readonly Guid tenant = Guid.NewGuid();
    private readonly Guid user = Guid.NewGuid();
    private readonly ReleaseFilesHandler handler;

    public ReleaseFilesHandlerTest()
    {
        handler = new ReleaseFilesHandler(repository.Object, pubsub.Object, Mock.Of<ILogger<ReleaseFilesHandler>>());
    }

    private FileStorageAggregate Stored(Guid ownerTenant)
    {
        var aggregate = FileStorageAggregate.Create(Guid.NewGuid(), "salon-social-1.jpg", "common-areas", ownerTenant, Guid.NewGuid());
        aggregate.GetAndClearEvents();

        repository
            .Setup(r => r.FindAsync<FileStorageAggregate>(aggregate.Id, ownerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregate);

        return aggregate;
    }

    [Fact]
    public async Task HandleAsync_ActiveFiles_DeactivatesAndSavesEach()
    {
        var first = Stored(tenant);
        var second = Stored(tenant);

        await handler.HandleAsync(FilesReleasedDomainEvent.Create(Guid.NewGuid(), [first.Id, second.Id], user, tenant), CancellationToken.None);

        Assert.False(first.IsActive);
        Assert.False(second.IsActive);
        Assert.Equal(user, first.UpdatedBy);
        repository.Verify(r => r.UpdateAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.UpdateAsync(second, It.IsAny<CancellationToken>()), Times.Once);
        pubsub.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_FileOfAnotherTenant_LeavesItActive()
    {
        var foreign = Stored(Guid.NewGuid());

        await handler.HandleAsync(FilesReleasedDomainEvent.Create(Guid.NewGuid(), [foreign.Id], user, tenant), CancellationToken.None);

        Assert.True(foreign.IsActive);
        repository.Verify(r => r.UpdateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyReleased_SkipsIt()
    {
        var released = Stored(tenant);
        released.Delete(Guid.NewGuid());
        released.GetAndClearEvents();

        await handler.HandleAsync(FilesReleasedDomainEvent.Create(Guid.NewGuid(), [released.Id, Guid.NewGuid()], user, tenant), CancellationToken.None);

        repository.Verify(r => r.UpdateAsync(It.IsAny<FileStorageAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
