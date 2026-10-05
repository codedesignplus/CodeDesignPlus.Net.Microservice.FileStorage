using Microsoft.Extensions.Options;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;

/// <summary>
/// Guarda un archivo con su registro, a nombre de una copropiedad y de un usuario explícitos.
/// </summary>
/// <remarks>
/// Es el camino único de toda subida (regla 56 de <c>rules/</c>). La subida por REST lo envía con la copropiedad y el
/// usuario de la sesión; el gRPC lo envía con los que trae la petición, porque quien llama es un job o un consumidor de
/// otro microservicio y no tiene JWT (pendings/260).
/// </remarks>
/// <param name="Id">Id del registro. Lo genera quien sube y es también la carpeta del archivo.</param>
/// <param name="Stream">Contenido del archivo.</param>
/// <param name="File">Nombre original, con extensión. No se renombra.</param>
/// <param name="Target">La carpeta: un nombre seguro según <see cref="FileScope.IsValidTarget"/>.</param>
/// <param name="Tenant">Copropiedad dueña del archivo. En los targets de plataforma no se usa: van al contenedor de la plataforma.</param>
/// <param name="UploadedBy">Usuario a nombre de quien se guarda.</param>
public record StoreFileCommand(Guid Id, Stream Stream, string File, string Target, Guid Tenant, Guid UploadedBy) : IRequest<StoredFileDto>;

public class Validator : AbstractValidator<StoreFileCommand>
{
    public Validator(IOptions<FileScopeOptions> scope)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Stream).NotNull();
        RuleFor(x => x.File).NotEmpty();
        RuleFor(x => x.Target).NotEmpty();
        RuleFor(x => x.UploadedBy).NotEmpty();

        When(x => x.Stream != null, () =>
        {
            RuleFor(x => x.Stream.Length).GreaterThan(0);
        });

        // Un archivo de copropiedad sin copropiedad acabaría en el contenedor de la plataforma, visible para cualquiera
        // que tenga el id. Solo los targets de plataforma pueden llegar sin ella.
        When(x => x.Target != null && !scope.Value.IsPlatformTarget(x.Target), () =>
        {
            RuleFor(x => x.Tenant).NotEmpty();
        });
    }
}
