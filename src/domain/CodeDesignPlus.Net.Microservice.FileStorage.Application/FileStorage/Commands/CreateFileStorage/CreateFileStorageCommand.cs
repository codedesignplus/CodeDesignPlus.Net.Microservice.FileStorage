
namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;

[DtoGenerator]
public record CreateFileStorageCommand(Guid Id, Stream Stream, string File, string Target, bool Renowned) : IRequest;

public class Validator : AbstractValidator<CreateFileStorageCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
        RuleFor(x => x.Stream).NotEmpty().NotNull();
        RuleFor(x => x.File).NotEmpty().NotNull().WithMessage("File name cannot be empty.");
        RuleFor(x => x.Target).NotEmpty().NotNull().WithMessage("Target cannot be empty.");
        RuleFor(x => x.Renowned).NotNull().WithMessage("Renowned cannot be null.");

         When(x => x.Stream != null, () =>
        {
            RuleFor(x => x.Stream.Length).GreaterThan(0).WithMessage("Stream length must be greater than 0.");
        });
    }
}
