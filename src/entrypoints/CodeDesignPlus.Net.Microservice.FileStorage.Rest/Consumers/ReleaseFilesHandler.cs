using CodeDesignPlus.Net.Core.Abstractions.Contracts;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;
using CodeDesignPlus.Net.PubSub.Abstractions;
using CodeDesignPlus.Net.PubSub.Abstractions.Attributes;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Consumers;

/// <summary>
/// Desactiva los archivos que un micro dejó de usar; el job de limpieza los borra al vencer la retención.
/// </summary>
/// <remarks>
/// Lo publica cualquier micro al borrar un registro o quitarle archivos (pendings/172). Es un contrato del SDK y no un
/// gemelo: así este micro de plataforma no depende de quién usa sus archivos.
/// <para>
/// Solo se buscan los archivos en la copropiedad del evento: un micro no puede soltar los de otra. Y puede llegar más
/// de una vez: un archivo que ya no existe o ya está desactivado se salta.
/// </para>
/// </remarks>
[QueueName<FileStorageAggregate>("ReleaseFilesHandler")]
public class ReleaseFilesHandler(IFileStorageRepository repository, IPubSub pubsub, ILogger<ReleaseFilesHandler> logger) : IEventHandler<FilesReleasedDomainEvent>
{
    public async Task HandleAsync(FilesReleasedDomainEvent data, CancellationToken token)
    {
        foreach (var id in data.Files)
        {
            var aggregate = await repository.FindAsync<FileStorageAggregate>(id, data.Tenant, token);

            if (aggregate is null || !aggregate.IsActive)
            {
                logger.LogWarning("File {FileId} released by {AggregateId} was not found active in tenant {Tenant}", id, data.AggregateId, data.Tenant);
                continue;
            }

            aggregate.Delete(data.ReleasedBy);

            await repository.UpdateAsync(aggregate, token);

            await pubsub.PublishAsync(aggregate.GetAndClearEvents(), token);
        }

        logger.LogInformation("{Count} files released by {AggregateId} in tenant {Tenant}", data.Files.Count, data.AggregateId, data.Tenant);
    }
}
