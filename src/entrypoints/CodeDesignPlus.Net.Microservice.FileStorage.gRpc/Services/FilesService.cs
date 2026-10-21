using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.ImportFile;
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

    /// <summary>
    /// Registra un archivo que otro sistema dejó en un contenedor de la misma cuenta, copiándolo en el servidor: el
    /// contenido no viaja en el mensaje, así que no le aplica el tope de 16 MB de <see cref="Upload"/>.
    /// </summary>
    /// <param name="request">El archivo, con su id, copropiedad, usuario, target, nombre y de dónde se toma.</param>
    /// <param name="context">El contexto de la llamada.</param>
    /// <returns>El id, el target y el nombre con que quedó guardado.</returns>
    public override async Task<UploadFileResponse> Import(ImportFileRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Id"));

        if (!Guid.TryParse(request.Tenant, out var tenant))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Tenant"));

        if (!Guid.TryParse(request.UploadedBy, out var uploadedBy))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid UploadedBy"));

        var command = new ImportFileCommand(id, request.SourceContainer, request.SourcePath, request.FileName, request.Target, tenant, uploadedBy);

        var result = await mediator.Send(command, context.CancellationToken);

        return new UploadFileResponse
        {
            Id = result.Id.ToString(),
            Target = result.Target,
            FileName = result.FileName,
        };
    }
}
