namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;

[DtoGenerator]
public record DeleteFileStorageCommand(Guid Id) : IRequest;

public class Validator : AbstractValidator<DeleteFileStorageCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
    }
}
