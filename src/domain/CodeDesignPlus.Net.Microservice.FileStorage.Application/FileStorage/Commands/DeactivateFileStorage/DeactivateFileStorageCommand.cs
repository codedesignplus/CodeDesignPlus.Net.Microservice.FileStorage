namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeactivateFileStorage;

/// <summary>
/// Comando para desactivar (soft delete) un FileStorage sin eliminar los archivos físicos del proveedor.
/// Los archivos quedan marcados con IsActive=false y pueden ser eliminados posteriormente por un job de limpieza.
/// </summary>
/// <param name="Id">El identificador único del FileStorage a desactivar.</param>
[DtoGenerator]
public record DeactivateFileStorageCommand(Guid Id) : IRequest;

public class Validator : AbstractValidator<DeactivateFileStorageCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
    }
}
