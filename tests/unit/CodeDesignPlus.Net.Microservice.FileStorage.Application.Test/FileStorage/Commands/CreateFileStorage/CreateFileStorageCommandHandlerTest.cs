using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using MediatR;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.CreateFileStorage;

/// <summary>
/// La subida por REST no guarda por sí misma: delega en <see cref="StoreFileCommand"/> con la copropiedad y el usuario
/// de la sesión, para que REST y gRPC pasen por el mismo sitio (pendings/260).
/// </summary>
public class CreateFileStorageCommandHandlerTest
{
    private readonly Mock<IMediator> mediator = new();
    private readonly Mock<IUserContext> user = new();
    private readonly CreateFileStorageCommandHandler handler;

    public CreateFileStorageCommandHandlerTest()
    {
        handler = new CreateFileStorageCommandHandler(mediator.Object, user.Object);
    }

    [Fact]
    public async Task Handle_RequestIsNull_ThrowsInvalidRequest()
    {
        var exception = await Assert.ThrowsAsync<CodeDesignPlusException>(() => handler.Handle(null!, CancellationToken.None));

        Assert.Equal(Errors.InvalidRequest.GetMessage(), exception.Message);
        Assert.Equal(Errors.InvalidRequest.GetCode(), exception.Code);
        Assert.Equal(Layer.Application, exception.Layer);
        mediator.Verify(x => x.Send(It.IsAny<StoreFileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>El archivo se guarda a nombre de la copropiedad y del usuario de la sesión, con el mismo contenido.</summary>
    [Fact]
    public async Task Handle_ValidRequest_SendsStoreFileWithSessionTenantAndUser()
    {
        var tenant = Guid.NewGuid();
        var userId = Guid.NewGuid();
        user.SetupGet(x => x.Tenant).Returns(tenant);
        user.SetupGet(x => x.IdUser).Returns(userId);
        var stream = new MemoryStream([1, 2]);
        var request = new CreateFileStorageCommand(Guid.NewGuid(), stream, "photo.jpg", "common-areas", true);
        mediator.Setup(x => x.Send(It.IsAny<StoreFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileDto { Id = request.Id, Target = request.Target, FileName = request.File });

        await handler.Handle(request, CancellationToken.None);

        mediator.Verify(x => x.Send(
            It.Is<StoreFileCommand>(c =>
                c.Id == request.Id &&
                c.Stream == stream &&
                c.File == "photo.jpg" &&
                c.Target == "common-areas" &&
                c.Tenant == tenant &&
                c.UploadedBy == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
