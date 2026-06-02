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

        var exist = await repository.ExistsAsync<FileStorageAggregate>(request.Id, cancellationToken);

        ApplicationGuard.IsFalse(exist, Errors.FileStorageDoesNotExists);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, cancellationToken);

        aggregate.Delete(user.IdUser);

        await repository.UpdateAsync(aggregate, cancellationToken);

        await pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken);
    }
}
