using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeDesignPlus.Net.Core.Abstractions.Models.Pager;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.DataTransferObjects;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetAllFileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageById;
using CodeDesignPlus.Net.Microservice.FileStorage.Rest.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test.Controllers;

public class FileStorageControllerTest
{
    private readonly Mock<IMediator> mediatorMock;
    private readonly FileStorageController controller;

    public FileStorageControllerTest()
    {
        mediatorMock = new Mock<IMediator>();
        controller = new FileStorageController(mediatorMock.Object);
    }

    [Fact]
    public async Task GetFiles_ReturnsOkResult()
    {
        // Arrange
        var criteria = new C.Criteria();
        var cancellationToken = CancellationToken.None;
        var expectedResult = Pagination<FileStorageDto>.Create([new() { Id = Guid.NewGuid() }], 1, 10, 0);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllFileStorageQuery>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.GetFiles(criteria, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetFileById_ReturnsOkResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var expectedResult = new FileStorageDto() { Id = Guid.NewGuid() };
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFileStorageByIdQuery>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.GetFileById(id, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult_WhenFileIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var fileName = "test.txt";
        var target = "target-folder";
        var renowned = true;
        var cancellationToken = CancellationToken.None;

        var stream = new MemoryStream();
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1);

        // Act
        var result = await controller.Upload(id, fileMock.Object, target, renowned, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("File uploaded successfully.", okResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        IFormFile file = null!;
        var target = "target-folder";
        var renowned = true;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await controller.Upload(id, file, target, renowned, cancellationToken);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var id = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var target = "target-folder";
        var renowned = true;
        var cancellationToken = CancellationToken.None;

        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await controller.Upload(id, fileMock.Object, target, renowned, cancellationToken);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequestResult.Value);
    }
}
