using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;

/// <summary>
/// La subida por REST: guarda el archivo a nombre de la copropiedad y el usuario de la sesión.
/// </summary>
/// <remarks>
/// No guarda nada por sí misma: delega en <see cref="StoreFileCommand"/>, que es el mismo camino que usa el gRPC, para
/// que el target permitido, la carpeta por id y el registro se decidan en un solo sitio (pendings/260).
/// <c>Renowned</c> se conserva en el contrato REST pero no se usa: con la carpeta por id nunca se renombra (pendings/167).
/// </remarks>
public class CreateFileStorageCommandHandler(IMediator mediator, IUserContext user) : IRequestHandler<CreateFileStorageCommand>
{
    public async Task Handle(CreateFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var command = new StoreFileCommand(request.Id, request.Stream, request.File, request.Target, user.Tenant, user.IdUser);

        await mediator.Send(command, cancellationToken);
    }
}
