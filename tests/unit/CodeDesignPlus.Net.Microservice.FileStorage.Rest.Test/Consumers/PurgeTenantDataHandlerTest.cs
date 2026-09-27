using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeDesignPlus.Net.Core.Abstractions;
using CodeDesignPlus.Net.Exceptions;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.Consumers;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.DomainEvents;
using CodeDesignPlus.Net.Mongo.Abstractions;
using Microsoft.Extensions.Logging;
using M = CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test.Consumers;

/// <summary>
/// Al purgarse una copropiedad no puede quedar en este micro ningún archivo ni documento suyo (regla 47).
/// </summary>
public class PurgeTenantDataHandlerTest
{
    private readonly Mock<IFileStorageRepository> repository = new();
    private readonly Mock<IFileStorage> fileStorage = new();
    private readonly List<string> calls = [];
    private readonly Guid tenant = Guid.NewGuid();
    private readonly PurgeTenantDataHandler handler;

    public PurgeTenantDataHandlerTest()
    {
        repository
            .Setup(r => r.DeleteByTenantAsync<FileStorageAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("documents"))
            .ReturnsAsync(3);

        handler = new PurgeTenantDataHandler(repository.Object, fileStorage.Object, Mock.Of<ILogger<PurgeTenantDataHandler>>());
    }

    private void FilesDeleted(params bool[] success)
    {
        fileStorage
            .Setup(f => f.DeleteTenantAsync(tenant, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("files"))
            .ReturnsAsync(success.Select(ok => new M.Response(new M.File(tenant.ToString()), TypeProviders.AzureBlobProvider) { Success = ok, Message = ok ? string.Empty : "Forbidden" }).ToArray());
    }

    /// <summary>
    /// Los tipos persistidos del micro que guardan la copropiedad en una propiedad <c>Tenant</c>.
    /// </summary>
    /// <remarks>
    /// Sale del dominio por reflexión y no de una lista escrita a mano: así un agregado nuevo entra solo en la
    /// prueba, y si nadie lo añade al consumidor, la prueba falla.
    /// </remarks>
    private static HashSet<Type> TypesWithTenant() => typeof(FileStorageAggregate).Assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && typeof(IEntityBase).IsAssignableFrom(type))
        .Where(type => type.GetProperty("Tenant", BindingFlags.Public | BindingFlags.Instance)?.PropertyType is { } property && (property == typeof(Guid) || property == typeof(Guid?)))
        .ToHashSet();

    [Fact]
    public async Task HandleAsync_TenantPurged_DeletesEveryTypeWithTenant()
    {
        // Arrange
        FilesDeleted(true);

        // Act
        await handler.HandleAsync(TenantPurgedDomainEvent.Create(tenant, "Malpelo XXI"), CancellationToken.None);

        // Assert
        var purged = repository.Invocations
            .Where(invocation => invocation.Method.Name == nameof(IRepositoryBase.DeleteByTenantAsync) && (Guid)invocation.Arguments[0] == tenant)
            .Select(invocation => invocation.Method.GetGenericArguments()[0])
            .ToHashSet();

        Assert.Empty(TypesWithTenant().Except(purged).Select(type => type.Name));
    }

    [Fact]
    public async Task HandleAsync_TenantPurged_DeletesTheFilesBeforeTheDocuments()
    {
        // Arrange
        FilesDeleted(true, true);

        // Act
        await handler.HandleAsync(TenantPurgedDomainEvent.Create(tenant, "Malpelo XXI"), CancellationToken.None);

        // Assert
        Assert.Equal(["files", "documents"], calls);
    }

    [Fact]
    public async Task HandleAsync_AProviderFails_ThrowsAndKeepsTheDocumentsForTheRetry()
    {
        // Arrange
        FilesDeleted(true, false);

        // Act
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.HandleAsync(TenantPurgedDomainEvent.Create(tenant, "Malpelo XXI"), CancellationToken.None));

        // Assert
        Assert.Equal(Application.Errors.TenantFilesNotDeleted.Code, exception.Code);
        repository.Verify(r => r.DeleteByTenantAsync<FileStorageAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TypesWithTenant_Domain_FindsTheAggregates()
    {
        // Act
        var types = TypesWithTenant();

        // Assert
        Assert.Contains(typeof(FileStorageAggregate), types);
    }
}
