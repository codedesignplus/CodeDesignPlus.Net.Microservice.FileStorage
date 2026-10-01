namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeactivateFileStorage;

/// <summary>
/// Handler para desactivar un FileStorage (soft delete).
/// Solo marca el registro como IsActive=false sin eliminar los archivos físicos del storage provider.
/// </summary>
public class DeactivateFileStorageCommandHandler(
    IFileStorageRepository repository,
    IUserContext user,
    IPubSub pubsub) : IRequestHandler<DeactivateFileStorageCommand>
{
    public async Task Handle(DeactivateFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        // Solo un archivo de la copropiedad en sesión, o uno de plataforma que subió quien lo desactiva (pendings/168).
        var aggregate = await repository.FindVisibleAsync(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileStorageDoesNotExists);
        ApplicationGuard.IsTrue(aggregate!.Tenant == Guid.Empty && aggregate.CreatedBy != user.IdUser, Errors.FileStorageDoesNotExists);

        aggregate.Delete(user.IdUser);

        await repository.UpdateAsync(aggregate, cancellationToken);

        await pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken);
    }
}
