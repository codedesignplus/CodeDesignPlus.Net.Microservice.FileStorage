using CodeDesignPlus.Net.Exceptions.Guards;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.DomainEvents;
using CodeDesignPlus.Net.PubSub.Abstractions;
using CodeDesignPlus.Net.PubSub.Abstractions.Attributes;
using Errors = CodeDesignPlus.Net.Microservice.FileStorage.Application.Errors;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Consumers;

/// <summary>
/// Al purgarse una copropiedad, borra sus archivos del almacenamiento y los registros que los describen.
/// </summary>
/// <remarks>
/// Lo publica ms-tenants cuando vence el plazo para restaurar una copropiedad eliminada, y puede llegar más de una
/// vez: borrar lo que ya no está es inofensivo, y un contenedor que no existe cuenta como borrado.
/// <para>
/// Primero los archivos y después los registros. Si un proveedor no pudo borrar, se lanza para que el mensaje se
/// reintente, y los registros siguen ahí para saber qué quedó; al revés quedarían archivos sin nadie que los nombre.
/// </para>
/// <para>
/// Vive en el entrypoint Rest porque este micro no tiene AsyncWorker: el SDK registra los consumidores que encuentra
/// en los ensamblados cargados, y el Rest ya es donde corre su trabajo de fondo (<c>FileStorageCleanupJob</c>).
/// </para>
/// <para>
/// <c>PurgeTenantDataHandlerTest</c> recorre el dominio y exige que se purgue todo tipo con <c>Tenant</c>: un
/// agregado nuevo que no se añada aquí hace fallar la prueba (regla 47).
/// </para>
/// </remarks>
[QueueName<FileStorageAggregate>("PurgeTenantDataHandler")]
public class PurgeTenantDataHandler(IFileStorageRepository repository, IFileStorage fileStorage, ILogger<PurgeTenantDataHandler> logger) : IEventHandler<TenantPurgedDomainEvent>
{
    public async Task HandleAsync(TenantPurgedDomainEvent data, CancellationToken token)
    {
        var responses = await fileStorage.DeleteTenantAsync(data.AggregateId, token);

        var failures = responses.Where(response => !response.Success).Select(response => $"{response.Provider}: {response.Message}").ToList();

        ApplicationGuard.IsTrue(failures.Count > 0, Errors.TenantFilesNotDeleted.With(string.Join("; ", failures)));

        var deleted = await repository.DeleteByTenantAsync<FileStorageAggregate>(data.AggregateId, token);

        logger.LogInformation("Tenant {TenantId} purged: files deleted from {Providers} providers and {Deleted} documents deleted", data.AggregateId, responses.Length, deleted);
    }
}
