namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;

/// <summary>
/// Lo que queda guardado tras una subida: el id del registro, su target y el nombre con que quedó el archivo.
/// </summary>
public class StoredFileDto : IDtoBase
{
    public Guid Id { get; set; }
    public string Target { get; set; } = null!;

    /// <summary>
    /// El nombre con que quedó guardado (el <c>FullName</c> del archivo), el mismo que se usa para descargarlo.
    /// </summary>
    public string FileName { get; set; } = null!;
}
