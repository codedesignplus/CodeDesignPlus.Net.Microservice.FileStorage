
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.Test;

public class FileStorageAggregateTest
{
    private readonly Metadata metadata;
    private readonly FileDetail fileDeatilValueObject;
    private readonly Domain.ValueObjects.File fileValueObject;

    public FileStorageAggregateTest()
    {
        
        var fileDetail = new File.Storage.Abstractions.Models.FileDetail(new Uri("http://example.com"), "custom", "fake.txt", TypeProviders.LocalProvider);
        var file = new File.Storage.Abstractions.Models.File("fake.txt") {
            Detail = fileDetail
        };
        var response = new File.Storage.Abstractions.Models.Response(file, TypeProviders.LocalProvider);

        this.metadata = Metadata.Create(file.Detail.File, file.Detail.Target, file.Detail.Uri, file.Detail.UriDownload, file.Detail.UriViewInBrowser, response.Provider);
        this.fileDeatilValueObject = FileDetail.Create(file.Extension, file.FullName, file.Name, this.metadata, file.Size, file.Version.ToString(), file.Renowned, file.Mime);
        this.fileValueObject = new Domain.ValueObjects.File(response.Success, response.Message, this.fileDeatilValueObject, response.Provider);

    }

    [Fact]
    public void Create_ShouldInitializeFileStorageAggregate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var file = "test-file.txt";
        var target = "test-target";
        var tenant = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var aggregate = FileStorageAggregate.Create(id, file, target, tenant, createdBy);

        // Assert
        var events = aggregate.GetAndClearEvents();
        Assert.Equal(id, aggregate.Id);
        Assert.Equal(file, aggregate.File);
        Assert.Equal(target, aggregate.Target);
        Assert.Equal(tenant, aggregate.Tenant);
        Assert.Equal(createdBy, aggregate.CreatedBy);
        Assert.False(aggregate.IsDeleted);

        Assert.NotNull(events);
        Assert.Contains(events, e => e is FileStorageCreatedDomainEvent);
    }

    [Fact]
    public void AddFile_ShouldAddFileToAggregate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var file = "test-file.txt";
        var target = "test-target";
        var tenant = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var aggregate = FileStorageAggregate.Create(id, file, target, tenant, createdBy);

        var updatedBy = Guid.NewGuid();

        // Act
        aggregate.AddFile(this.fileValueObject, updatedBy);

        // Assert
        var events = aggregate.GetAndClearEvents();
        Assert.Single(aggregate.Files);
        Assert.Equal(this.fileValueObject, aggregate.Files.First());
        Assert.Equal(updatedBy, aggregate.UpdatedBy);
        Assert.NotNull(aggregate.UpdatedAt);
        Assert.NotNull(events);
        Assert.Contains(events, e => e is FileStorageAddedDomainEvent);
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void AddFile_ShouldThrowException_WhenFileAlreadyExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var file = "test-file.txt";
        var target = "test-target";
        var tenant = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var aggregate = FileStorageAggregate.Create(id, file, target, tenant, createdBy);

        var updatedBy = Guid.NewGuid();

        aggregate.AddFile(this.fileValueObject, updatedBy);

        // Act & Assert
        var exception = Assert.Throws<CodeDesignPlusException>(() => aggregate.AddFile(this.fileValueObject, updatedBy));

        Assert.Equal(Errors.FileAlreadyExists.GetMessage(), exception.Message);
        Assert.Equal(Errors.FileAlreadyExists.GetCode(), exception.Code);
        Assert.Equal(Layer.Domain, exception.Layer);
    }

    [Fact]
    public void Delete_ShouldMarkAggregateAsDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        var file = "test-file.txt";
        var target = "test-target";
        var tenant = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var aggregate = FileStorageAggregate.Create(id, file, target, tenant, createdBy);

        var updatedBy = Guid.NewGuid();

        // Act
        aggregate.Delete(updatedBy);

        // Assert
        var events = aggregate.GetAndClearEvents();

        Assert.True(aggregate.IsDeleted);
        Assert.Equal(updatedBy, aggregate.UpdatedBy);
        Assert.NotNull(aggregate.UpdatedAt);
        Assert.NotNull(events);
        Assert.Contains(events, e => e is FileStorageDeletedDomainEvent);
        Assert.Equal(2, events.Count);
    }
}
