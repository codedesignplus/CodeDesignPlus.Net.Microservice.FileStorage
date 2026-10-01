using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;

public class DeleteFileStorageCommandHandler(IFileStorageRepository repository, IFileStorage fileStorage, IUserContext user, IPubSub pubsub) : IRequestHandler<DeleteFileStorageCommand>
{
    public async Task Handle(DeleteFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);
        
        var aggregate = await repository.FindVisibleAsync(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileStorageDoesNotExists);
        ApplicationGuard.IsTrue(aggregate!.Tenant == Guid.Empty && aggregate.CreatedBy != user.IdUser, Errors.FileStorageDoesNotExists);

        aggregate.Delete(user.IdUser);

        await repository.UpdateAsync(aggregate, cancellationToken);

        // Se borra el blob real de cada archivo, no el nombre original: ese puede ser el de otro registro (pendings/167).
        foreach (var file in aggregate.Files)
        {
            var (name, folder) = FileScope.BlobOf(file, aggregate.Target);

            await fileStorage.DeleteAsync(name, folder, aggregate.Tenant, cancellationToken);
        }

        await pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken);
    }
}
