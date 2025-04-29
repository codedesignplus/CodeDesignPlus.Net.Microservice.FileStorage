using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.DomainEvents;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Moq;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.CreateFileStorage;

public class CreateFileStorageCommandHandlerTest
{
    private readonly Mock<IFileStorageRepository> repositoryMock;
    private readonly Mock<IUserContext> userContextMock;
    private readonly Mock<IPubSub> pubSubMock;
    private readonly Mock<IFileStorage> fileStorageMock;
    private readonly Mock<IMapper> mapperMock;
    private readonly CreateFileStorageCommandHandler handler;

    public CreateFileStorageCommandHandlerTest()
    {
        repositoryMock = new Mock<IFileStorageRepository>();
        userContextMock = new Mock<IUserContext>();
        pubSubMock = new Mock<IPubSub>();
        fileStorageMock = new Mock<IFileStorage>();
        mapperMock = new Mock<IMapper>();

        handler = new CreateFileStorageCommandHandler(
            repositoryMock.Object,
            userContextMock.Object,
            pubSubMock.Object,
            fileStorageMock.Object,
            mapperMock.Object
        );
    }

    [Fact]
    public async Task Handle_RequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        CreateFileStorageCommand request = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(request, cancellationToken));

        Assert.Equal(Errors.InvalidRequest.GetMessage(), exception.Message);
        Assert.Equal(Errors.InvalidRequest.GetCode(), exception.Code);
        Assert.Equal(Layer.Application, exception.Layer);
    }

    [Fact]
    public async Task Handle_AggregateDoesNotExist_CreatesNewAggregate()
    {
        // Arrange
        var request = new CreateFileStorageCommand(Guid.NewGuid(), new MemoryStream(), "fake.txt", "custom", false);

        var cancellationToken = CancellationToken.None;

        repositoryMock
            .Setup(repo => repo.FindAsync<FileStorageAggregate>(request.Id, cancellationToken))
            .ReturnsAsync((FileStorageAggregate)null!);

        userContextMock.SetupGet(user => user.Tenant).Returns(Guid.NewGuid());
        userContextMock.SetupGet(user => user.IdUser).Returns(Guid.NewGuid());

        var fileDetail = new File.Storage.Abstractions.Models.FileDetail(new Uri("http://example.com"), "custom", "fake.txt", TypeProviders.LocalProvider);
        var file = new File.Storage.Abstractions.Models.File("fake.txt") {
            Detail = fileDetail
        };
        var response = new File.Storage.Abstractions.Models.Response(file, TypeProviders.LocalProvider);

        var metadata = Metadata.Create(file.Detail.File, file.Detail.Target, file.Detail.Uri, file.Detail.UriDownload, file.Detail.UriViewInBrowser, response.Provider);
        var fileDeatilValueObject = FileDetail.Create(file.Extension, file.FullName, file.Name, metadata, file.Size, file.Version.ToString(), file.Renowned, file.Mime);
        var fileValueObject = new Domain.ValueObjects.File(response.Success, response.Message, fileDeatilValueObject, response.Provider);

        fileStorageMock
            .Setup(fs => fs.UploadAsync(request.Stream, request.File, request.Target, request.Renowned, cancellationToken))
            .ReturnsAsync([response]);

        mapperMock
            .Setup(mapper => mapper.Map<Domain.ValueObjects.File>(It.IsAny<File.Storage.Abstractions.Models.Response>()))
            .Returns(fileValueObject);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        repositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<FileStorageAggregate>(), cancellationToken), Times.Once);
        pubSubMock.Verify(pubsub => pubsub.PublishAsync(It.IsAny<List<FileStorageAddedDomainEvent>>(), cancellationToken), Times.AtMostOnce);
        pubSubMock.Verify(pubsub => pubsub.PublishAsync(It.IsAny<List<FileStorageCreatedDomainEvent>>(), cancellationToken), Times.AtMostOnce);
    }

    [Fact]
    public async Task Handle_AggregateExists_UpdatesAggregate()
    {
        // Arrange
        var request = new CreateFileStorageCommand(Guid.NewGuid(), new MemoryStream(), "fake.txt", "custom", false);

        var cancellationToken = CancellationToken.None;

        var existingAggregate = FileStorageAggregate.Create(request.Id, request.File, request.Target,  Guid.NewGuid(), Guid.NewGuid());
        
        userContextMock.SetupGet(user => user.Tenant).Returns(Guid.NewGuid());
        userContextMock.SetupGet(user => user.IdUser).Returns(Guid.NewGuid());

        var fileDetail = new File.Storage.Abstractions.Models.FileDetail(new Uri("http://example.com"), "custom", "fake.txt", TypeProviders.LocalProvider);
        var file = new File.Storage.Abstractions.Models.File("fake.txt") {
            Detail = fileDetail
        };
        var response = new File.Storage.Abstractions.Models.Response(file, TypeProviders.LocalProvider);

        var metadata = Metadata.Create(file.Detail.File, file.Detail.Target, file.Detail.Uri, file.Detail.UriDownload, file.Detail.UriViewInBrowser, response.Provider);
        var fileDeatilValueObject = FileDetail.Create(file.Extension, file.FullName, file.Name, metadata, file.Size, file.Version.ToString(), file.Renowned, file.Mime);
        var fileValueObject = new Domain.ValueObjects.File(response.Success, response.Message, fileDeatilValueObject, response.Provider);

        repositoryMock
            .Setup(repo => repo.FindAsync<FileStorageAggregate>(request.Id, cancellationToken))
            .ReturnsAsync(existingAggregate);

        fileStorageMock
            .Setup(fs => fs.UploadAsync(request.Stream, request.File, request.Target, request.Renowned, cancellationToken))
            .ReturnsAsync([response]);

        mapperMock
            .Setup(mapper => mapper.Map<Domain.ValueObjects.File>(It.IsAny<File.Storage.Abstractions.Models.Response>()))
            .Returns(fileValueObject);

        // Act
        await handler.Handle(request, cancellationToken);

        // Assert
        repositoryMock.Verify(repo => repo.CreateAsync(existingAggregate, cancellationToken), Times.Once);
        pubSubMock.Verify(pubsub => pubsub.PublishAsync(It.IsAny<List<FileStorageAddedDomainEvent>>(), cancellationToken), Times.AtMostOnce);
        pubSubMock.Verify(pubsub => pubsub.PublishAsync(It.IsAny<List<FileStorageCreatedDomainEvent>>(), cancellationToken), Times.AtMostOnce);
    }
}
