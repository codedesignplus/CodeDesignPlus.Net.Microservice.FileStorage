using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application;

public class Errors : IErrorCodes
{
    public static readonly Error UnknownError = new("200");
    public static readonly Error InvalidRequest = new("201");
    public static readonly Error FileStorageDoesNotExists = new("202");

    public static readonly Error FileNotFound = new("203");

    public static readonly Error TenantFilesNotDeleted = new("204");

    /// <summary>
    /// El target de la subida no está en la lista de targets permitidos.
    /// </summary>
    public static readonly Error InvalidTarget = new("205");

    /// <summary>
    /// La importación pide un contenedor de origen que el despliegue no declara en <c>FileScope:ImportSources</c>, o una
    /// ruta que sale de él.
    /// </summary>
    public static readonly Error ImportSourceNotAllowed = new("206");

    /// <summary>
    /// El almacenamiento no pudo copiar el archivo que se importa: no existe en el origen o la copia no terminó. Lleva la
    /// causa que dio el almacenamiento (<c>{0}</c>), para que quien importa la vea en el error y no solo en el log.
    /// </summary>
    public static readonly Error ImportFailed = new("207");

    /// <summary>
    /// El archivo no tiene extensión o es de un tipo que el catálogo MIME no conoce: el registro no lo aceptaría, y se
    /// rechaza antes de subirlo o copiarlo para no dejar un archivo sin registro (Kappali pendings/526).
    /// </summary>
    public static readonly Error FileTypeNotAccepted = new("208");
}
