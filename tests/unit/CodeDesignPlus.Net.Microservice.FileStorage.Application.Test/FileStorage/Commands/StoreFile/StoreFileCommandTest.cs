using System.IO;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using FluentValidation.TestHelper;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.StoreFile;

/// <summary>
/// Lo que la subida exige antes de llegar al handler. Quien llama por gRPC no tiene sesión, así que el usuario y la
/// copropiedad tienen que venir en la petición.
/// </summary>
public class StoreFileCommandTest
{
    private readonly Validator validator = new(Options.Create(new FileScopeOptions { PlatformTargets = ["users"] }));

    private static StoreFileCommand Valid() => new(Guid.NewGuid(), new MemoryStream([1, 2, 3]), "soat.pdf", "vehicle-documents", Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_EmptyId_HasError()
    {
        validator.TestValidate(Valid() with { Id = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_NullStream_HasError()
    {
        validator.TestValidate(Valid() with { Stream = null! }).ShouldHaveValidationErrorFor(x => x.Stream);
    }

    [Fact]
    public void Validator_EmptyStream_HasError()
    {
        validator.TestValidate(Valid() with { Stream = new MemoryStream([]) }).ShouldHaveValidationErrorFor(x => x.Stream.Length);
    }

    [Fact]
    public void Validator_EmptyFile_HasError()
    {
        validator.TestValidate(Valid() with { File = string.Empty }).ShouldHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void Validator_EmptyTarget_HasError()
    {
        validator.TestValidate(Valid() with { Target = string.Empty }).ShouldHaveValidationErrorFor(x => x.Target);
    }

    /// <summary>Un archivo siempre se guarda a nombre de alguien.</summary>
    [Fact]
    public void Validator_EmptyUploadedBy_HasError()
    {
        validator.TestValidate(Valid() with { UploadedBy = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.UploadedBy);
    }

    /// <summary>Un archivo de copropiedad sin copropiedad caería en el contenedor de la plataforma.</summary>
    [Fact]
    public void Validator_CondominiumTargetWithoutTenant_HasError()
    {
        validator.TestValidate(Valid() with { Tenant = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.Tenant);
    }

    /// <summary>Los targets de plataforma (los de la configuración) no son de ninguna copropiedad: pueden llegar sin ella.</summary>
    [Fact]
    public void Validator_PlatformTargetWithoutTenant_HasNoErrors()
    {
        validator.TestValidate(Valid() with { Target = "users", Tenant = Guid.Empty }).ShouldNotHaveAnyValidationErrors();
    }
}
