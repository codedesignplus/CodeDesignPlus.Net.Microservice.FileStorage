using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CodeDesignPlus.Net.File.Storage.Abstractions.Providers;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;
using Microsoft.Extensions.Logging;
using M = CodeDesignPlus.Net.File.Storage.Abstractions.Models;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test.Controllers;

/// <summary>
/// La descarga de «Cotización (1).pdf» respondía 500: Kestrel no escribe una cabecera con caracteres fuera de ASCII y el
/// nombre iba tal cual en <c>filename</c> (Kappali pendings/526). Se prueba contra un Kestrel de verdad, que es quien la
/// rechaza.
/// </summary>
public class DownloadContentDispositionTest
{
    private const string FileName = "Cotización (1).pdf";

    [Fact]
    public void For_NameWithAccents_IsAsciiWithTheUtf8NameAndAFallback()
    {
        // Act
        var header = DownloadContentDisposition.For(false, FileName);

        // Assert
        Assert.Equal((true, true, true), (header.All(char.IsAscii), header.StartsWith("attachment;"), header.Contains("filename*=UTF-8''Cotizaci%C3%B3n%20%281%29.pdf")));
    }

    [Theory]
    [InlineData(true, "inline;")]
    [InlineData(false, "attachment;")]
    public void For_ViewInBrowser_ChoosesTheDisposition(bool viewInBrowser, string expected)
    {
        // Act
        var header = DownloadContentDisposition.For(viewInBrowser, "minutes.pdf");

        // Assert
        Assert.StartsWith(expected, header);
    }

    [Fact]
    public async Task Kestrel_HeaderBuiltForANameWithAccents_IsWritten()
    {
        // Act
        var (status, value) = await GetAsync(DownloadContentDisposition.For(false, FileName));

        // Assert
        Assert.Equal((HttpStatusCode.OK, FileName), (status, value));
    }

    /// <summary>
    /// La forma anterior, con el nombre tal cual: Kestrel la rechaza. Deja escrito por qué la cabecera va codificada.
    /// </summary>
    [Fact]
    public async Task Kestrel_NameWithAccentsWrittenAsIs_FailsWith500()
    {
        // Act
        var (status, _) = await GetAsync($"attachment; filename=\"{FileName}\"");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, status);
    }

    /// <summary>
    /// La descarga del controlador escribe la cabecera codificada, no el nombre tal cual.
    /// </summary>
    [Fact]
    public async Task Download_NameWithAccents_WritesAnAsciiContentDisposition()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var id = Guid.NewGuid();
        var response = new M.Response(new M.File(FileName), TypeProviders.AzureBlobProvider) { Stream = new System.IO.MemoryStream([1, 2, 3]), Success = true };

        mediator.Setup(x => x.Send(It.Is<DownloadQuery>(q => q.Id == id), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var controller = new FileStorageController(mediator.Object) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };

        // Act
        await controller.Download(id, false, CancellationToken.None);

        // Assert
        Assert.Equal(DownloadContentDisposition.For(false, FileName), controller.Response.Headers.ContentDisposition.ToString());
    }

    private static async Task<(HttpStatusCode Status, string? FileName)> GetAsync(string contentDisposition)
    {
        var builder = WebApplication.CreateSlimBuilder();

        // Un puerto libre en el bucle local; Listen manda sobre las URL que traiga la configuración del entorno.
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();

        await using var app = builder.Build();

        app.MapGet("/download", (HttpContext context) =>
        {
            context.Response.Headers.Append("Content-Disposition", contentDisposition);

            return Results.Bytes([1, 2, 3], "application/pdf");
        });

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

        using var client = new HttpClient();
        using var response = await client.GetAsync($"{address}/download");

        await app.StopAsync();

        return (response.StatusCode, response.Content.Headers.ContentDisposition?.FileNameStar);
    }
}
