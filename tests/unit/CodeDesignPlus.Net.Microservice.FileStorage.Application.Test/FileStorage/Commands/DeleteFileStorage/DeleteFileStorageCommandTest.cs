using System;
using FluentValidation.TestHelper;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.DeleteFileStorage;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage.Commands.DeleteFileStorage;

public class DeleteFileStorageCommandTest
{
    [Fact]
    public void Validator_Should_Have_Error_When_Id_Is_Empty()
    {
        // Arrange
        var validator = new Validator();
        var command = new DeleteFileStorageCommand(Guid.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_Should_Not_Have_Error_When_Id_Is_Valid()
    {
        // Arrange
        var validator = new Validator();
        var command = new DeleteFileStorageCommand(Guid.NewGuid());

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }
}
