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
    /// El almacenamiento no pudo copiar el archivo que se importa: no existe en el origen o la copia no terminó.
    /// </summary>
    public static readonly Error ImportFailed = new("207");
}
