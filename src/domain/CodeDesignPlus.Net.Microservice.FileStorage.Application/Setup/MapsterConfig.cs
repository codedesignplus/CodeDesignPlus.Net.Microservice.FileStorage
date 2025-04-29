using CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Setup;

public static class MapsterConfigFileStorage
{
    public static void Configure()
    {
        TypeAdapterConfig<File.Storage.Abstractions.Models.FileDetail, Metadata>
            .NewConfig()
            .MapWith(src => Metadata.Create(src.File, src.Target, src.Uri, src.UriDownload, src.UriViewInBrowser, src.Provider));

        TypeAdapterConfig<File.Storage.Abstractions.Models.File, FileDetail>
            .NewConfig()
            .MapWith(src => FileDetail.Create(src.Extension, src.FullName, src.Name, src.Detail.Adapt<Metadata>(), src.Size, src.Version.ToString(), src.Renowned, src.Mime));

        TypeAdapterConfig<File.Storage.Abstractions.Models.Response, Domain.ValueObjects.File>
            .NewConfig()
            .MapWith(src => Domain.ValueObjects.File.Create(src.Success, src.Message, src.File.Adapt<FileDetail>(), src.Provider));

        TypeAdapterConfig<FileStorageAggregate, FileStorageDto>
            .NewConfig();
            // .MapWith(src => new FileStorageDto
            // {
            //     Id = src.Id,
            //     Files = src.Files.Select(x => x.Adapt<FileDto>()).ToList()
            // });
    }
}
