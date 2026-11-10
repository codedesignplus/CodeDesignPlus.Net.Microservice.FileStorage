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

    /// <summary>
    /// Busca un archivo en un contenedor exacto: el de una copropiedad o el de la plataforma (<see cref="Guid.Empty"/>).
    /// </summary>
    /// <remarks>
    /// Es la búsqueda de quien escribe (subir, importar, soltar). La del SDK por tenant exige un tenant y rechaza
    /// <see cref="Guid.Empty"/>, que aquí es un contenedor válido: con ella ningún archivo de plataforma llegaba a crearse.
    /// </remarks>
    /// <param name="id">El id del archivo.</param>
    /// <param name="container">La copropiedad dueña, o <see cref="Guid.Empty"/> para la plataforma.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El archivo no borrado de ese contenedor, o null.</returns>
    Task<FileStorageAggregate?> FindInContainerAsync(Guid id, Guid container, CancellationToken cancellationToken);

    /// <summary>
    /// Borra del todo el registro de un archivo de un contenedor exacto, también el de la plataforma, esté o no
    /// desactivado o borrado (es la limpieza de los archivos desactivados).
    /// </summary>
    /// <param name="id">El id del archivo.</param>
    /// <param name="container">La copropiedad dueña, o <see cref="Guid.Empty"/> para la plataforma.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task DeleteFromContainerAsync(Guid id, Guid container, CancellationToken cancellationToken);
}
