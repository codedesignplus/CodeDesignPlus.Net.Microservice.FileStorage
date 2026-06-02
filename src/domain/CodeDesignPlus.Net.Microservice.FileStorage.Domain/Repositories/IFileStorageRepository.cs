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
}