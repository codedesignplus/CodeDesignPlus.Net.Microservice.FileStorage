namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;

public interface IFileStorageRepository : IRepositoryBase
{
    /// <summary>
    /// Obtiene archivos inactivos que superan el período de retención especificado.
    /// Este método está diseñado para uso interno del sistema (jobs de limpieza)
    /// y busca a través de todos los tenants.
    /// </summary>
    /// <param name="retentionDays">Número de días de retención</param>
    /// <param name="batchSize">Número máximo de registros a retornar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de agregados inactivos que exceden el período de retención</returns>
    Task<IEnumerable<FileStorageAggregate>> GetInactiveFilesForCleanupAsync(
        int retentionDays,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca un archivo que la sesión puede ver: el de su copropiedad o uno de plataforma (contenedor
    /// <see cref="Guid.Empty"/>). Nunca el de otra copropiedad (pendings/168).
    /// </summary>
    Task<FileStorageAggregate?> FindVisibleAsync(Guid id, Guid sessionTenant, CancellationToken cancellationToken);
}
