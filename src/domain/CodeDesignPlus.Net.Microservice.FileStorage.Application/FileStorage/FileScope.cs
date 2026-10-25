using System.Text.RegularExpressions;

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
/// El contenedor es la copropiedad en sesión, salvo en las carpetas de plataforma que diga la configuración
/// (<see cref="FileScopeOptions"/>), que no son de ninguna copropiedad: viven en el contenedor de la plataforma (<see cref="Guid.Empty"/>), los ve cualquiera que tenga
/// el id y solo los desactiva quien los subió. Así la foto no depende de la copropiedad que estaba abierta al subirla, ni
/// se borra cuando esa copropiedad se purga.
/// </para>
/// </remarks>
public static class FileScope
{
    /// <summary>
    /// El formato de una carpeta: minúsculas, números y guiones, empezando por letra o número, hasta 64 caracteres.
    /// </summary>
    private static readonly Regex TargetFormat = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Si la carpeta tiene un nombre seguro. Cualquier producto elige sus carpetas; el micro solo impide que una carpeta
    /// salga de su sitio («..», «/», «\») o sea un nombre raro (pendings/302).
    /// </summary>
    /// <param name="target">La carpeta de la subida.</param>
    /// <returns><see langword="true"/> si el nombre es seguro.</returns>
    public static bool IsValidTarget(string? target) => target is not null && TargetFormat.IsMatch(target);

    /// <summary>
    /// Si la ruta de un archivo que se importa se queda dentro de su contenedor: relativa, sin «..» y con «/» como
    /// separador.
    /// </summary>
    /// <param name="path">La ruta dentro del contenedor de origen.</param>
    /// <returns><see langword="true"/> si la ruta es segura.</returns>
    public static bool IsValidImportPath(string? path)
        => !string.IsNullOrWhiteSpace(path)
            && !path.StartsWith('/')
            && !path.Contains('\\')
            && !path.Split('/').Any(segment => segment is "" or "." or "..");

    /// <summary>
    /// El contenedor en que se guarda un archivo de la carpeta: el de la plataforma si la configuración la marca como
    /// de plataforma; si no, el de la copropiedad en sesión.
    /// </summary>
    public static Guid TenantFor(string target, Guid sessionTenant, FileScopeOptions options)
        => options.IsPlatformTarget(target) ? Guid.Empty : sessionTenant;

    /// <summary>
    /// Si el registro aceptará el archivo: tiene extensión y el catálogo MIME conoce su tipo. Se mira antes de subirlo o de
    /// copiarlo; si no, el blob quedaba en el almacenamiento y el registro lo rechazaba después (Kappali pendings/526).
    /// </summary>
    /// <param name="fileName">El nombre del archivo, con su extensión.</param>
    /// <returns><see langword="true"/> si el archivo se puede registrar.</returns>
    public static bool IsAcceptedFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var file = new CodeDesignPlus.Net.File.Storage.Abstractions.Models.File(fileName);

        return !string.IsNullOrEmpty(file.Extension) && file.Mime is not null;
    }

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
