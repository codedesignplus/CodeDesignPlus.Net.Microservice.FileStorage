using CodeDesignPlus.Net.File.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.ImportFile;

/// <summary>
/// Copia el archivo en el servidor a <c>{contenedor}/{target}/{id}/{nombre}</c>, crea su registro y después borra el
/// origen.
/// </summary>
/// <remarks>
/// El orden es el que permite reintentar: si algo falla antes de guardar el registro, el origen sigue ahí y la siguiente
/// llamada vuelve a copiar (la copia pisa la anterior); si falla al borrar el origen, el registro ya existe y la siguiente
/// llamada con el mismo id solo vuelve a intentar el borrado.
/// </remarks>
/// <param name="repository">Los registros de archivos.</param>
/// <param name="pubsub">El bus de eventos.</param>
/// <param name="fileStorage">El almacenamiento.</param>
/// <param name="mapper">El mapeador.</param>
/// <param name="scope">Dónde se guardan los archivos.</param>
/// <param name="logger">El registro.</param>
public class ImportFileCommandHandler(IFileStorageRepository repository, IPubSub pubsub, IFileStorage fileStorage, IMapper mapper, IOptions<FileScopeOptions> scope, ILogger<ImportFileCommandHandler> logger) : IRequestHandler<ImportFileCommand, StoredFileDto>
{
    /// <summary>
    /// Importa el archivo.
    /// </summary>
    /// <param name="request">El archivo y de dónde se toma.</param>
    /// <param name="cancellationToken">El token de cancelación.</param>
    /// <returns>El id, el target y el nombre con que quedó guardado.</returns>
    public async Task<StoredFileDto> Handle(ImportFileCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        ApplicationGuard.IsFalse(FileScope.IsValidTarget(request.Target), Errors.InvalidTarget);
        ApplicationGuard.IsFalse(scope.Value.IsImportSource(request.SourceContainer), Errors.ImportSourceNotAllowed);
        ApplicationGuard.IsFalse(FileScope.IsValidImportPath(request.SourcePath), Errors.ImportSourceNotAllowed);

        var tenant = FileScope.TenantFor(request.Target, request.Tenant, scope.Value);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, tenant, cancellationToken);

        if (aggregate is not null && aggregate.Files.Count > 0)
        {
            // Ya se importó: una entrega repetida del aviso o un reintento tras fallar el borrado del origen.
            await ReleaseSourceAsync(request, cancellationToken);

            return Stored(aggregate, aggregate.Files[0].FileDetail?.FullName ?? request.File);
        }

        var responses = await fileStorage.ImportAsync(request.SourceContainer, request.SourcePath, request.File, FileScope.FolderFor(request.Target, request.Id), tenant, cancellationToken);

        var failed = responses.Length == 0 || responses.Any(x => !x.Success);

        if (failed)
        {
            logger.LogWarning("Import of {SourcePath} from {SourceContainer} as file {Id} failed: {Messages}", request.SourcePath, request.SourceContainer, request.Id, string.Join("; ", responses.Select(x => $"{x.Provider}: {x.Message}")));
        }

        ApplicationGuard.IsTrue(failed, Errors.ImportFailed);

        var isNew = aggregate is null;

        aggregate ??= FileStorageAggregate.Create(request.Id, request.File, request.Target, tenant, request.UploadedBy);

        string? storedName = null;

        foreach (var item in responses)
        {
            var map = mapper.Map<Domain.ValueObjects.File>(item);

            aggregate.AddFile(map, request.UploadedBy);

            storedName ??= map.FileDetail?.FullName;
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

        await ReleaseSourceAsync(request, cancellationToken);

        return Stored(aggregate, storedName ?? request.File);
    }

    /// <summary>
    /// Borra el origen. Si no se puede, el archivo ya está registrado: queda en el log y lo borra el siguiente intento.
    /// </summary>
    private async Task ReleaseSourceAsync(ImportFileCommand request, CancellationToken cancellationToken)
    {
        var responses = await fileStorage.DeleteImportSourceAsync(request.SourceContainer, request.SourcePath, cancellationToken);

        foreach (var failure in responses.Where(x => !x.Success))
        {
            logger.LogWarning("The source {SourcePath} of file {Id} could not be deleted from {SourceContainer} in {Provider}: {Message}", request.SourcePath, request.Id, request.SourceContainer, failure.Provider, failure.Message);
        }
    }

    private static StoredFileDto Stored(FileStorageAggregate aggregate, string fileName) => new()
    {
        Id = aggregate.Id,
        Target = aggregate.Target,
        FileName = fileName,
    };
}
