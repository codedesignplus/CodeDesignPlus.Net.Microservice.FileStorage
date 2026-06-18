using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;

public class DownloadQueryHandler(IFileStorageRepository repository, IUserContext user, IFileStorage fileStorage) : IRequestHandler<DownloadQuery, File.Storage.Abstractions.Models.Response>
{
    public async Task<File.Storage.Abstractions.Models.Response> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        var exist = await repository.ExistsAsync<FileStorageAggregate>(request.Id, user.Tenant, cancellationToken);

        DomainGuard.IsFalse(exist, Errors.FileNotFound);

        var file = await fileStorage.DownloadAsync(request.File, request.Target, user.Tenant, cancellationToken);

        // Si el aggregate existe pero el `file`/`target` no resuelve a un blob real (p. ej. el caller
        // pasó el name sin extensión), la SDK devuelve un Response sin File/Stream poblados.
        // Convertimos esa señal en FileNotFound — el filtro global la mapea a 404 — en vez de dejar
        // que el controller explote con NRE al leer `result.File.FullName`.
        ApplicationGuard.IsNull(file, Errors.FileNotFound);
        ApplicationGuard.IsNull(file.File, Errors.FileNotFound);
        ApplicationGuard.IsNull(file.Stream, Errors.FileNotFound);

        return file;
    }
}
