using Microsoft.Extensions.Options;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.ImportFile;

/// <summary>
/// Registra un archivo que otro sistema dejó en un contenedor de la misma cuenta de almacenamiento —por ejemplo, la
/// grabación de una llamada—: lo copia en el servidor a <c>{contenedor}/{target}/{id}/{nombre}</c>, crea su registro y
/// borra el origen (regla 56 de <c>rules/</c>).
/// </summary>
/// <remarks>
/// Es la tercera puerta de la regla 56, junto a la subida por REST y por gRPC: el contenido no pasa por el proceso, así
/// que no le aplica el tope de tamaño del mensaje gRPC. Solo se importa de los contenedores que el despliegue declara en
/// <see cref="FileScopeOptions.ImportSources"/>; nunca del de otra copropiedad.
/// </remarks>
/// <param name="Id">Id del registro. Lo genera quien importa y es también la carpeta del archivo.</param>
/// <param name="SourceContainer">Contenedor donde lo dejó el otro sistema.</param>
/// <param name="SourcePath">Ruta del archivo dentro de <paramref name="SourceContainer"/>.</param>
/// <param name="File">Nombre con el que queda el archivo, con extensión.</param>
/// <param name="Target">La carpeta: un nombre seguro según <see cref="FileScope.IsValidTarget"/>.</param>
/// <param name="Tenant">Copropiedad dueña del archivo.</param>
/// <param name="UploadedBy">Usuario a nombre de quien se guarda.</param>
public record ImportFileCommand(Guid Id, string SourceContainer, string SourcePath, string File, string Target, Guid Tenant, Guid UploadedBy) : IRequest<StoredFileDto>;

/// <summary>
/// Validador de <see cref="ImportFileCommand"/>.
/// </summary>
public class Validator : AbstractValidator<ImportFileCommand>
{
    /// <summary>
    /// Las reglas del comando.
    /// </summary>
    /// <param name="scope">Dónde se guardan los archivos.</param>
    public Validator(IOptions<FileScopeOptions> scope)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SourceContainer).NotEmpty();
        RuleFor(x => x.SourcePath).NotEmpty();
        RuleFor(x => x.File).NotEmpty();
        RuleFor(x => x.Target).NotEmpty();
        RuleFor(x => x.UploadedBy).NotEmpty();

        // Igual que en la subida: un archivo de copropiedad sin copropiedad acabaría en el contenedor de la plataforma.
        When(x => x.Target != null && !scope.Value.IsPlatformTarget(x.Target), () =>
        {
            RuleFor(x => x.Tenant).NotEmpty();
        });
    }
}
