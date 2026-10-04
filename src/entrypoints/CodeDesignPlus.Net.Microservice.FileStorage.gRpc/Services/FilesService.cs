using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;

namespace CodeDesignPlus.Net.Microservice.FileStorage.gRpc.Services;

/// <summary>
/// Guarda los archivos que genera o recibe otro microservicio, con su registro, igual que si se subieran por REST
/// (pendings/260, regla 56 de <c>rules/</c>).
/// </summary>
/// <remarks>
/// Lo llaman jobs y consumidores, sin JWT: la copropiedad y el usuario viajan en la petición y no en la cabecera.
/// </remarks>
public class FilesService(IMediator mediator) : Files.FilesBase
{
    public override async Task<UploadFileResponse> Upload(UploadFileRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));

        if (!Guid.TryParse(request.Tenant, out var tenant))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Tenant"));

        if (!Guid.TryParse(request.UploadedBy, out var uploadedBy))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UploadedBy"));

        var command = new StoreFileCommand(id, new MemoryStream(request.Content.ToByteArray()), request.FileName, request.Target, tenant, uploadedBy);

        var result = await mediator.Send(command, context.CancellationToken);

        return new UploadFileResponse
        {
            Id = result.Id.ToString(),
            Target = result.Target,
            FileName = result.FileName,
        };
    }
}
