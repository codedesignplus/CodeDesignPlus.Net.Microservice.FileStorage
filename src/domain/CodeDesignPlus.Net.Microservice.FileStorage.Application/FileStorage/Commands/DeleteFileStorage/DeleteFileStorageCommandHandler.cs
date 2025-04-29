using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;

public class DeleteFileStorageCommandHandler(IFileStorageRepository repository, IFileStorage fileStorage, IUserContext user, IPubSub pubsub) : IRequestHandler<DeleteFileStorageCommand>
{
    public async Task Handle(DeleteFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);
        
        var exist = await repository.ExistsAsync<FileStorageAggregate>(request.Id, cancellationToken);

        ApplicationGuard.IsFalse(exist, Errors.FileStorageDoesNotExists);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, cancellationToken);

        aggregate.Delete(user.IdUser);

        await repository.UpdateAsync(aggregate, cancellationToken);

        await fileStorage.DeleteAsync(aggregate.File, aggregate.Target, cancellationToken);

        await pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken);
    }
}
