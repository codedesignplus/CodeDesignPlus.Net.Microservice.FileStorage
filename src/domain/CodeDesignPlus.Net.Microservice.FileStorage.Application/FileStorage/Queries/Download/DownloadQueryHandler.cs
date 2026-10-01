using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;

public class DownloadQueryHandler(IFileStorageRepository repository, IUserContext user, IFileStorage fileStorage) : IRequestHandler<DownloadQuery, File.Storage.Abstractions.Models.Response>
{
    public async Task<File.Storage.Abstractions.Models.Response> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        // Se descarga el archivo del registro, nunca un nombre o una carpeta que lleguen en la dirección: con un id
        // válido cualquiera se podía leer cualquier archivo de la copropiedad (pendings/168).
        var aggregate = await repository.FindVisibleAsync(request.Id, user.Tenant, cancellationToken);

        ApplicationGuard.IsNull(aggregate, Errors.FileNotFound);
        ApplicationGuard.IsFalse(aggregate!.IsActive, Errors.FileNotFound);

        var stored = aggregate.Files.FirstOrDefault(x => x.Success);

        ApplicationGuard.IsNull(stored, Errors.FileNotFound);

        var (name, folder) = FileScope.BlobOf(stored!, aggregate.Target);

        var file = await fileStorage.DownloadAsync(name, folder, aggregate.Tenant, cancellationToken);

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
