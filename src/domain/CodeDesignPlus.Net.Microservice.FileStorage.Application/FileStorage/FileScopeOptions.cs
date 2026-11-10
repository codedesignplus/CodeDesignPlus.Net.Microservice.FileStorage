namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;

/// <summary>
/// Configuración de dónde se guardan los archivos (sección <see cref="Section"/>).
/// </summary>
/// <remarks>
/// El micro es transversal: no sabe qué carpetas usa cada producto. Lo único que necesita saber es cuáles son de la
/// plataforma —no de una copropiedad— para guardarlas en su contenedor, y eso lo dice el despliegue (pendings/302).
/// </remarks>
public class FileScopeOptions
{
    /// <summary>La sección de configuración.</summary>
    public const string Section = "FileScope";

    /// <summary>
    /// Carpetas cuyos archivos son de la plataforma y van al contenedor de la plataforma (<see cref="Guid.Empty"/>).
    /// </summary>
    public string[] PlatformTargets { get; set; } = [];

    /// <summary>
    /// El contenedor del almacenamiento donde viven los archivos de la plataforma.
    /// </summary>
    /// <remarks>
    /// El registro de un archivo de plataforma lleva <see cref="Guid.Empty"/> como copropiedad: así lo reconocen la
    /// lectura (<c>FindVisibleAsync</c>), los permisos de borrado y la purga, que nunca lo toca. Pero el almacenamiento
    /// del SDK usa la copropiedad como contenedor y rechaza <see cref="Guid.Empty"/>, así que el blob va a este id, fijo
    /// en el despliegue. No debe ser el id de ninguna copropiedad: su purga se llevaría los archivos de la plataforma.
    /// </remarks>
    public Guid PlatformContainer { get; set; }

    /// <summary>
    /// Contenedores de la misma cuenta desde los que se puede importar un archivo (<c>ImportFileCommand</c>): los que llena
    /// otro sistema, como el de las grabaciones de llamadas. Vacío, no se importa nada.
    /// </summary>
    /// <remarks>
    /// Es una lista cerrada a propósito: el gRPC no pide token, y sin ella cualquiera dentro del clúster podría copiar a su
    /// copropiedad el archivo de otra con solo nombrar su contenedor.
    /// </remarks>
    public string[] ImportSources { get; set; } = [];

    /// <summary>Si se puede importar desde ese contenedor.</summary>
    /// <param name="container">El contenedor de origen.</param>
    /// <returns><see langword="true"/> si está en <see cref="ImportSources"/>.</returns>
    public bool IsImportSource(string? container)
        => container is not null && ImportSources.Contains(container, StringComparer.Ordinal);

    /// <summary>
    /// El contenedor del almacenamiento de un archivo según la copropiedad de su registro: el de la plataforma para los
    /// registros de plataforma (<see cref="Guid.Empty"/>), y el de la copropiedad para los demás.
    /// </summary>
    /// <param name="owner">La copropiedad del registro, o <see cref="Guid.Empty"/> si es de plataforma.</param>
    /// <returns>El contenedor del blob.</returns>
    public Guid ContainerOf(Guid owner) => owner == Guid.Empty ? PlatformContainer : owner;

    /// <summary>Si la carpeta es de la plataforma.</summary>
    /// <param name="target">La carpeta de la subida.</param>
    /// <returns><see langword="true"/> si está en <see cref="PlatformTargets"/>.</returns>
    public bool IsPlatformTarget(string? target)
        => target is not null && PlatformTargets.Contains(target, StringComparer.Ordinal);
}
