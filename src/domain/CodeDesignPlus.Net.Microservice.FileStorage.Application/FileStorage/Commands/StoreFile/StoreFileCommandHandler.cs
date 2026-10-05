using Microsoft.Extensions.Options;
using CodeDesignPlus.Net.File.Storage.Abstractions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;

/// <summary>
/// Sube el archivo a <c>{contenedor}/{target}/{id}/{nombre original}</c> y crea o actualiza su registro.
/// </summary>
public class StoreFileCommandHandler(IFileStorageRepository repository, IPubSub pubsub, IFileStorage fileStorage, IMapper mapper, IOptions<FileScopeOptions> scope) : IRequestHandler<StoreFileCommand, StoredFileDto>
{
    public async Task<StoredFileDto> Handle(StoreFileCommand request, CancellationToken cancellationToken)
    {
        ApplicationGuard.IsNull(request, Errors.InvalidRequest);

        ApplicationGuard.IsFalse(FileScope.IsValidTarget(request.Target), Errors.InvalidTarget);

        var tenant = FileScope.TenantFor(request.Target, request.Tenant, scope.Value);

        var aggregate = await repository.FindAsync<FileStorageAggregate>(request.Id, tenant, cancellationToken);
        var isNew = aggregate == null;

        aggregate ??= FileStorageAggregate.Create(request.Id, request.File, request.Target, tenant, request.UploadedBy);

        // Cada archivo en su carpeta: {target}/{id}/{nombre original}. Con la carpeta por id el nombre ya no choca con
        // el de otro archivo, así que nunca se renombra (pendings/167).
        var response = await fileStorage.UploadAsync(request.Stream, request.File, FileScope.FolderFor(request.Target, request.Id), false, tenant, cancellationToken);

        string? storedName = null;

        foreach (var item in response)
        {
            if (item != null)
            {
                var map = mapper.Map<Domain.ValueObjects.File>(item);

                aggregate.AddFile(map, request.UploadedBy);

                storedName ??= map.FileDetail?.FullName;
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

        return new StoredFileDto
        {
            Id = aggregate.Id,
            Target = aggregate.Target,
            FileName = storedName ?? request.File,
        };
    }
}
