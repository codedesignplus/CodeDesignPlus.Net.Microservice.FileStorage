using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;
using Microsoft.Extensions.FileProviders;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;

public class CreateFileStorageCommandHandler(IFileStorageRepository repository, IUserContext user, IPubSub pubsub, IFileStorage fileStorage, IMapper mapper) : IRequestHandler<CreateFileStorageCommand>
{
    public async Task Handle(CreateFileStorageCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, user.Tenant, cancellationToken);
        var isNew = aggregate == null;

        aggregate ??= FileStorageAggregate.Create(request.Id, request.File, request.Target, user.Tenant, user.IdUser);

        var response = await fileStorage.UploadAsync(request.Stream, request.File, request.Target, request.Renowned, user.Tenant, cancellationToken);

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