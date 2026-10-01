namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;

/// <summary>
/// Dónde vive cada archivo y quién puede verlo (pendings/167 y 168).
/// </summary>
/// <remarks>
/// <para>
/// Un archivo se guarda en <c>{contenedor}/{target}/{id}/{nombre original}</c>. La carpeta del id lo hace único: dos
/// archivos con el mismo nombre ya no se pisan, y el nombre original se conserva para la descarga y los adjuntos.
/// </para>
/// <para>
/// El contenedor es la copropiedad en sesión, salvo en los targets de plataforma (la foto de un usuario), que no son de
/// ninguna copropiedad: viven en el contenedor de la plataforma (<see cref="Guid.Empty"/>), los ve cualquiera que tenga
/// el id y solo los desactiva quien los subió. Así la foto no depende de la copropiedad que estaba abierta al subirla, ni
/// se borra cuando esa copropiedad se purga.
/// </para>
/// </remarks>
public static class FileScope
{
    /// <summary>
    /// Targets cuyos archivos son de la plataforma, no de una copropiedad.
    /// </summary>
    public static readonly IReadOnlySet<string> PlatformTargets = new HashSet<string>(StringComparer.Ordinal)
    {
        "users",
    };

    /// <summary>
    /// Targets que acepta la subida. Uno nuevo se añade aquí y en la pantalla que lo usa.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedTargets = new HashSet<string>(StringComparer.Ordinal)
    {
        "users",
        "common-areas",
        "expense-invoices",
        "email-templates",
        "quotation-documents",
        "pqrs-attachments",
        "infraction-evidence",
        "infraction-appeal",
        "lease-contracts",
        "fee-exclusions",
        "ownership-proofs",
        "moving-inspections",
    };

    /// <summary>
    /// El contenedor en que se guarda un archivo del target, según la copropiedad en sesión.
    /// </summary>
    public static Guid TenantFor(string target, Guid sessionTenant)
        => PlatformTargets.Contains(target) ? Guid.Empty : sessionTenant;

    /// <summary>
    /// La carpeta del blob: el target y, dentro, una por archivo.
    /// </summary>
    public static string FolderFor(string target, Guid id) => $"{target}/{id}";

    /// <summary>
    /// El nombre y la carpeta reales del blob, tal como se guardaron al subirlo. Los archivos anteriores a la carpeta por
    /// id quedaron en <c>{target}/{nombre}</c>, y su metadato lo dice igual.
    /// </summary>
    public static (string File, string Target) BlobOf(Domain.ValueObjects.File file, string aggregateTarget)
        => (file.FileDetail.Metadata?.File ?? file.FileDetail.FullName, file.FileDetail.Metadata?.Target ?? aggregateTarget);
}
