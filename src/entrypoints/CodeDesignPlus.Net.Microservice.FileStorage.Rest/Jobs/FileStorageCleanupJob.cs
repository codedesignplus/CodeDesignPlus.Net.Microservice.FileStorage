using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.Hangfire.Abstractions;
using CodeDesignPlus.Net.Hangfire.Abstractions.Attributes;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain;
using CodeDesignPlus.Net.Microservice.FileStorage.Domain.Repositories;
using Hangfire;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Jobs;

/// <summary>
/// Job recurrente de Hangfire que elimina físicamente archivos que han estado inactivos (IsActive=false)
/// por más de un período de retención configurado (30 días por defecto).
/// </summary>
/// <remarks>
/// Este job se ejecuta semanalmente (domingos a las 2 AM UTC) para liberar espacio en el
/// storage provider. Los registros con IsActive=false son candidatos para eliminación hard delete.
/// </remarks>
[RecurringJobOptions(
    "0 2 * * 0",                    // Cron: Domingos a las 2 AM
    jobId: "file-storage-cleanup",   // ID único del job
    queue: "default",                // Cola de ejecución
    timezone: "UTC"                  // Zona horaria
)]
public class FileStorageCleanupJob(
    IFileStorageRepository repository,
    IFileStorage fileStorage,
    ILogger<FileStorageCleanupJob> logger) : IRecurrentJob
{
    private const int RetentionDays = 30;
    private const int BatchSize = 100;

    /// <summary>
    /// Ejecuta la limpieza de archivos inactivos que excedan el período de retención.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación proporcionado por Hangfire.</param>
    public async Task ExecuteAsync(IJobCancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando limpieza de archivos inactivos con retención de {RetentionDays} días",
            RetentionDays);

        // Obtener archivos inactivos usando el método especializado del repositorio
        var inactiveFiles = await repository.GetInactiveFilesForCleanupAsync(
            RetentionDays,
            BatchSize,
            cancellationToken.ShutdownToken);

        var deletedCount = 0;
        var totalFound = 0;

        foreach (var aggregate in inactiveFiles)
        {
            totalFound++;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation(
                    "Eliminando archivo {FileStorageId} (Target: {Target}) inactivo desde {UpdatedAt}",
                    aggregate.Id,
                    aggregate.Target,
                    aggregate.UpdatedAt);

                // El blob real de cada archivo, tal como se guardó: {target}/{id}/{nombre}. Borrar por el nombre original y
                // el target del registro se llevaba el archivo de otro registro con el mismo nombre (pendings/167).
                foreach (var file in aggregate.Files)
                {
                    var (name, folder) = FileScope.BlobOf(file, aggregate.Target);

                    await fileStorage.DeleteAsync(
                        name,
                        folder,
                        aggregate.Tenant,
                        cancellationToken.ShutdownToken);
                }

                await repository.DeleteAsync<FileStorageAggregate>(aggregate.Id, cancellationToken.ShutdownToken);

                deletedCount++;

                logger.LogInformation("Archivo {FileStorageId} eliminado correctamente", aggregate.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error al eliminar archivo {FileStorageId} (Target: {Target})",
                    aggregate.Id,
                    aggregate.Target);

            }
        }

        logger.LogInformation("Limpieza completada: {DeletedCount} archivos eliminados de {TotalFound} encontrados",
            deletedCount,
            totalFound);
    }
}
