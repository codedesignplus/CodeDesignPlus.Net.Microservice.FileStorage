using System.IO;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.ImportFile;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using CodeDesignPlus.Net.Microservice.FileStorage.gRpc;
using Google.Protobuf;

namespace CodeDesignPlus.Net.Microservice.FileStorage.gRpc.Test.Services;

/// <summary>
/// La subida por gRPC desde otro microservicio (pendings/260): traduce la petición al mismo comando que usa el REST, con
/// la copropiedad y el usuario que vienen en ella, y responde con lo que quedó guardado.
/// </summary>
public class FilesServiceTest
{
    private readonly Mock<IMediator> mediator = new();

    private static ServerCallContext Context() => TestServerCallContext.Create(
        "Upload", null, DateTime.UtcNow.AddMinutes(1), [], CancellationToken.None, "127.0.0.1", null, null, _ => Task.CompletedTask, () => null, _ => { });

    private static UploadFileRequest Request() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Tenant = Guid.NewGuid().ToString(),
        UploadedBy = Guid.NewGuid().ToString(),
        Target = "vehicle-documents",
        FileName = "soat.pdf",
        Content = ByteString.CopyFrom(1, 2, 3),
    };

    /// <summary>El comando lleva los datos de la petición tal cual, y la respuesta el nombre con que quedó guardado.</summary>
    [Fact]
    public async Task Upload_ValidRequest_SendsStoreFileAndReturnsStoredFile()
    {
        var request = Request();
        StoreFileCommand? sent = null;
        byte[]? content = null;
        mediator.Setup(x => x.Send(It.IsAny<StoreFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<StoredFileDto>, CancellationToken>((command, _) =>
            {
                sent = (StoreFileCommand)command;
                content = ((MemoryStream)sent.Stream).ToArray();
            })
            .ReturnsAsync(new StoredFileDto { Id = Guid.Parse(request.Id), Target = request.Target, FileName = "soat.pdf" });

        var response = await new FilesService(mediator.Object).Upload(request, Context());

        Assert.NotNull(sent);
        Assert.Equal(Guid.Parse(request.Id), sent.Id);
        Assert.Equal(Guid.Parse(request.Tenant), sent.Tenant);
        Assert.Equal(Guid.Parse(request.UploadedBy), sent.UploadedBy);
        Assert.Equal("vehicle-documents", sent.Target);
        Assert.Equal("soat.pdf", sent.File);
        Assert.Equal(new byte[] { 1, 2, 3 }, content);

        Assert.Equal(request.Id, response.Id);
        Assert.Equal("vehicle-documents", response.Target);
        Assert.Equal("soat.pdf", response.FileName);
    }

    /// <summary>Un id, una copropiedad o un usuario que no son Guid se rechazan antes de tocar nada.</summary>
    [Theory]
    [InlineData("id")]
    [InlineData("tenant")]
    [InlineData("uploadedBy")]
    public async Task Upload_InvalidGuid_ThrowsInvalidArgument(string field)
    {
        var request = Request();
        switch (field)
        {
            case "id": request.Id = "not-a-guid"; break;
            case "tenant": request.Tenant = string.Empty; break;
            default: request.UploadedBy = "x"; break;
        }

        var exception = await Assert.ThrowsAsync<RpcException>(() => new FilesService(mediator.Object).Upload(request, Context()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        mediator.Verify(x => x.Send(It.IsAny<StoreFileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ImportFileRequest ImportRequest() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Tenant = Guid.NewGuid().ToString(),
        UploadedBy = Guid.NewGuid().ToString(),
        Target = "assembly-recordings",
        FileName = "session-1-part-1.mp4",
        SourceContainer = "acs-recordings",
        SourcePath = "20261021/call/recording/0-mixed.mp4",
    };

    /// <summary>La importación llega al comando con los datos de la petición tal cual, y responde lo que quedó guardado.</summary>
    [Fact]
    public async Task Import_ValidRequest_SendsImportFileAndReturnsStoredFile()
    {
        var request = ImportRequest();
        ImportFileCommand? sent = null;
        mediator.Setup(x => x.Send(It.IsAny<ImportFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<StoredFileDto>, CancellationToken>((command, _) => sent = (ImportFileCommand)command)
            .ReturnsAsync(new StoredFileDto { Id = Guid.Parse(request.Id), Target = request.Target, FileName = request.FileName });

        var response = await new FilesService(mediator.Object).Import(request, Context());

        Assert.Equal(new ImportFileCommand(Guid.Parse(request.Id), "acs-recordings", "20261021/call/recording/0-mixed.mp4", "session-1-part-1.mp4", "assembly-recordings", Guid.Parse(request.Tenant), Guid.Parse(request.UploadedBy)), sent);
        Assert.Equal(request.Id, response.Id);
        Assert.Equal("assembly-recordings", response.Target);
        Assert.Equal("session-1-part-1.mp4", response.FileName);
    }

    /// <summary>Un id, una copropiedad o un usuario que no son Guid se rechazan antes de tocar nada.</summary>
    [Theory]
    [InlineData("id")]
    [InlineData("tenant")]
    [InlineData("uploadedBy")]
    public async Task Import_InvalidGuid_ThrowsInvalidArgument(string field)
    {
        var request = ImportRequest();
        switch (field)
        {
            case "id": request.Id = "not-a-guid"; break;
            case "tenant": request.Tenant = string.Empty; break;
            default: request.UploadedBy = "x"; break;
        }

        var exception = await Assert.ThrowsAsync<RpcException>(() => new FilesService(mediator.Object).Import(request, Context()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        mediator.Verify(x => x.Send(It.IsAny<ImportFileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
