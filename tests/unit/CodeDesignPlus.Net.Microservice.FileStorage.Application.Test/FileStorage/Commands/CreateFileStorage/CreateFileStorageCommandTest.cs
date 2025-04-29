using System;
using System.IO;
using System.Text;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.CreateFileStorage;
using FluentValidation.TestHelper;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.CreateFileStorage;

public class CreateFileStorageCommandTest
{
    private readonly Validator validator;

    public CreateFileStorageCommandTest()
    {
        validator = new Validator();
    }

    [Fact]
    public void Validator_Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new CreateFileStorageCommand(Guid.Empty, new MemoryStream(), "file.txt", "target", true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_Should_Have_Error_When_Stream_Is_Null()
    {
        var command = new CreateFileStorageCommand(Guid.NewGuid(), null!, "file.txt", "target", true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Stream);
    }

    [Fact]
    public void Validator_Should_Have_Error_When_File_Is_Empty()
    {
        var command = new CreateFileStorageCommand(Guid.NewGuid(), new MemoryStream(), string.Empty, "target", true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.File).WithErrorMessage("'File' must not be empty.");
    }

    [Fact]
    public void Validator_Should_Have_Error_When_Target_Is_Empty()
    {
        var command = new CreateFileStorageCommand(Guid.NewGuid(), new MemoryStream(), "file.txt", string.Empty, true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Target).WithErrorMessage("'Target' must not be empty.");
    }

    [Fact]
    public void Validator_Should_Have_Error_When_Renowned_Is_Null()
    {
        var command = new CreateFileStorageCommand(Guid.NewGuid(), new MemoryStream(), "file.txt", "target", false);
        var result = validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Renowned);
    }

    [Fact]
    public void Validator_Should_Have_Error_When_Stream_Length_Is_Zero()
    {
        var stream = new MemoryStream([]);
        var command = new CreateFileStorageCommand(Guid.NewGuid(), stream, "file.txt", "target", true);
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Stream.Length).WithErrorMessage("Stream length must be greater than 0.");
    }

    [Fact]
    public void Validator_Should_Not_Have_Error_For_Valid_Command()
    {
        var stream = new MemoryStream([1, 2, 3]);
        var command = new CreateFileStorageCommand(Guid.NewGuid(), stream, "file.txt", "target", true);
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
