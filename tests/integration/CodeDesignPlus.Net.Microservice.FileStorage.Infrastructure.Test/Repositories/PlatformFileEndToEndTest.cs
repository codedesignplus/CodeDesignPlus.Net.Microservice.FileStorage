using CodeDesignPlus.Net.Core.Abstractions.Contracts;
using CodeDesignPlus.Net.File.Storage.Abstractions;
using CodeDesignPlus.Net.File.Storage.Extensions;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Commands.StoreFile;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.Download;
using CodeDesignPlus.Net.Microservice.FileStorage.Application.Setup;
using CodeDesignPlus.Net.Mongo.Extensions;
using CodeDesignPlus.Net.PubSub.Abstractions;
using CodeDesignPlus.Net.Security.Abstractions;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Test.Repositories;

/// <summary>
/// A file of a platform folder goes all the way with the storage of the SDK (its local provider, on disk) and a real
/// Mongo: it is uploaded with no tenant and downloaded from any condominium, kept in the container of the platform.
/// </summary>
[Collection(MongoContainerFixture.Collection)]
public sealed class PlatformFileEndToEndTest : IDisposable
{
    private const string PlatformTarget = "platform-electronic-documents";

    private static readonly Guid PlatformContainer = Guid.Parse("00000000-f11e-0000-0000-000000000001");

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"ms-filestorage-{Guid.NewGuid():N}");

    private readonly ServiceProvider provider;

    public PlatformFileEndToEndTest(MongoContainerFixture fixture)
    {
        Directory.CreateDirectory(folder);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:Enable"] = "true",
                ["Mongo:ConnectionString"] = fixture.ConnectionString,
                ["Mongo:Database"] = $"db-ms-filestorage-{Guid.NewGuid():N}",
                ["Mongo:RegisterHealthCheck"] = "false",
                ["FileStorage:UriDownload"] = "http://localhost/api/FileStorage/download",
                ["FileStorage:Local:Enable"] = "true",
                ["FileStorage:Local:Folder"] = folder,
                ["FileStorage:AzureBlob:Enable"] = "false",
                ["FileStorage:AzureFile:Enable"] = "false",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Test"));
        services.AddMongo<Startup>(configuration);
        services.AddFileStorage(configuration);

        provider = services.BuildServiceProvider();

        MapsterConfigFileStorage.Configure();
    }

    private static IOptions<FileScopeOptions> Scope() => Options.Create(new FileScopeOptions { PlatformTargets = [PlatformTarget], PlatformContainer = PlatformContainer });

    [Fact]
    public async Task StoreAndDownload_PlatformFile_GoesToThePlatformContainerAndComesBackFromAnyCondominium()
    {
        // Arrange
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IFileStorageRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var id = Guid.NewGuid();
        var content = "<Invoice>SETP1</Invoice>"u8.ToArray();
        var store = new StoreFileCommandHandler(repository, Mock.Of<IPubSub>(), storage, new Mapper(TypeAdapterConfig.GlobalSettings), Scope());
        var download = new DownloadQueryHandler(repository, Mock.Of<IUserContext>(x => x.Tenant == Guid.NewGuid()), storage, Scope());

        await store.Handle(new StoreFileCommand(id, new MemoryStream(content), "SETP1.xml", PlatformTarget, Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        // Act
        var file = await download.Handle(new DownloadQuery(id), CancellationToken.None);

        // Assert
        using var read = new MemoryStream();

        await file.Stream!.CopyToAsync(read);
        file.Stream.Dispose();

        Assert.Equal(("<Invoice>SETP1</Invoice>", true), (System.Text.Encoding.UTF8.GetString(read.ToArray()), System.IO.File.Exists(Path.Combine(folder, PlatformContainer.ToString(), PlatformTarget, id.ToString(), "SETP1.xml"))));
    }

    public void Dispose()
    {
        provider.Dispose();

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
