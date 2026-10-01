using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Microsoft.Extensions.FileProviders;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;

public class CreateFileStorageCommandHandler(IFileStorageRepository repository, IUserContext user, IPubSub pubsub, IFileStorage fileStorage, IMapper mapper) : IRequestHandler<CreateFileStorageCommand>
{
    public async Task Handle(CreateFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        ApplicationGuard.IsFalse(FileScope.AllowedTargets.Contains(request.Target), Errors.TargetIsNotAllowed);

        var tenant = FileScope.TenantFor(request.Target, user.Tenant);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, tenant, cancellationToken);
        var isNew = aggregate == null;

        aggregate ??= FileStorageAggregate.Create(request.Id, request.File, request.Target, tenant, user.IdUser);

        // Cada archivo en su carpeta: {target}/{id}/{nombre original}. Con la carpeta por id el nombre ya no choca con
        // el de otro archivo, así que nunca se renombra (pendings/167).
        var response = await fileStorage.UploadAsync(request.Stream, request.File, FileScope.FolderFor(request.Target, request.Id), false, tenant, cancellationToken);

        foreach (var item in response)
        {
            if (item != null)
            {
                var map = mapper.Map<Domain.ValueObjects.File>(item);

                aggregate.AddFile(map, user.IdUser);
            }
        }

        if (isNew)
        {
            await repository.CreateAsync(aggregate, cancellationToken);
        }
        else
        {
            await repository.UpdateAsync(aggregate, cancellationToken);
        }

        await pubsub.PublishAsync(aggregate.GetAndClearEvents(), cancellationToken);
    }
}