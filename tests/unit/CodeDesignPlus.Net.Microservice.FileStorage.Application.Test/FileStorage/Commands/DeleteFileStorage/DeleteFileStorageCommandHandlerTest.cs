using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;
using Moq;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.DeleteFileStorage
{
    public class DeleteFileStorageCommandHandlerTest
    {
        private readonly Mock<IFileStorageRepository> repositoryMock;
        private readonly Mock<IFileStorage> fileStorageMock;
        private readonly Mock<IUserContext> userContextMock;
        private readonly Mock<IPubSub> pubSubMock;
        private readonly DeleteFileStorageCommandHandler handler;

        public DeleteFileStorageCommandHandlerTest()
        {
            repositoryMock = new Mock<IFileStorageRepository>();
            fileStorageMock = new Mock<IFileStorage>();
            userContextMock = new Mock<IUserContext>();
            pubSubMock = new Mock<IPubSub>();

            handler = new DeleteFileStorageCommandHandler(
                repositoryMock.Object,
                fileStorageMock.Object,
                userContextMock.Object,
                pubSubMock.Object
            );
        }

        [Fact]
        public async Task Handle_RequestIsNull_ThrowsInvalidRequestException()
        {
            // Arrange
            DeleteFileStorageCommand request = null!;
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(request, cancellationToken));

            Assert.Equal(Errors.InvalidRequest.GetMessage(), exception.Message);
            Assert.Equal(Errors.InvalidRequest.GetCode(), exception.Code);
            Assert.Equal(Layer.Application, exception.Layer);
        }

        [Fact]
        public async Task Handle_FileStorageDoesNotExist_ThrowsFileStorageDoesNotExistsException()
        {
            // Arrange
            var request = new DeleteFileStorageCommand(Guid.NewGuid());
            var cancellationToken = CancellationToken.None;

            repositoryMock
                .Setup(repo => repo.ExistsAsync<FileStorageAggregate>(request.Id, cancellationToken))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(request, cancellationToken));

            Assert.Equal(Errors.FileStorageDoesNotExists.GetMessage(), exception.Message);
            Assert.Equal(Errors.FileStorageDoesNotExists.GetCode(), exception.Code);
            Assert.Equal(Layer.Application, exception.Layer);
        }

        [Fact]
        public async Task Handle_ValidRequest_DeletesFileStorage()
        {
            // Arrange
            var request = new DeleteFileStorageCommand(Guid.NewGuid());
            var cancellationToken = CancellationToken.None;

            var aggregate = FileStorageAggregate.Create(request.Id, "fake.txt", "custom", Guid.NewGuid(), Guid.NewGuid());

            repositoryMock
                .Setup(repo => repo.ExistsAsync<FileStorageAggregate>(request.Id, cancellationToken))
                .ReturnsAsync(true);

            repositoryMock
                .Setup(repo => repo.FindAsync<FileStorageAggregate>(request.Id, cancellationToken))
                .ReturnsAsync(aggregate);

            userContextMock
                .SetupGet(user => user.IdUser)
                .Returns(Guid.NewGuid());

            // Act
            await handler.Handle(request, cancellationToken);

            // Assert
            repositoryMock.Verify(repo => repo.UpdateAsync(aggregate, cancellationToken), Times.Once);
            fileStorageMock.Verify(fs => fs.DeleteAsync(aggregate.File, aggregate.Target, cancellationToken), Times.Once);
            pubSubMock.Verify(pubsub => pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken), Times.AtMostOnce);
        }
    }
}