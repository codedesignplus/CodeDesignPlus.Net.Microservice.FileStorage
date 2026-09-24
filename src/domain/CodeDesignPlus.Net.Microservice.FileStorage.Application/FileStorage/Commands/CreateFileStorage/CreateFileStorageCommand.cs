
namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;

[DtoGenerator]
public record CreateFileStorageCommand(Guid Id, Stream Stream, string File, string Target, bool Renowned) : IRequest;

public class Validator : AbstractValidator<CreateFileStorageCommand>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
        RuleFor(x => x.Stream).NotEmpty().NotNull();
        RuleFor(x => x.File).NotEmpty().NotNull();
        RuleFor(x => x.Target).NotEmpty().NotNull();
        RuleFor(x => x.Renowned).NotNull();

         When(x => x.Stream != null, () =>
        {
            RuleFor(x => x.Stream.Length).GreaterThan(0);
        });
    }
}
