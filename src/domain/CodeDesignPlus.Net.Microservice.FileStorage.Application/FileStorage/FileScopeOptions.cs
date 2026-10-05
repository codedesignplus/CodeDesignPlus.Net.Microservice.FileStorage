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

    /// <summary>Si la carpeta es de la plataforma.</summary>
    /// <param name="target">La carpeta de la subida.</param>
    /// <returns><see langword="true"/> si está en <see cref="PlatformTargets"/>.</returns>
    public bool IsPlatformTarget(string? target)
        => target is not null && PlatformTargets.Contains(target, StringComparer.Ordinal);
}
