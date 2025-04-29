using NodaTime.Serialization.SystemTextJson;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test.Controllers;

public class FileStorageControllerTest : ServerBase<Program>, IClassFixture<Server<Program>>
{
    private readonly System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);


    public FileStorageControllerTest(Server<Program> server) : base(server)
    {
        server.InMemoryCollection = (x) =>
        {
            x.Add("Vault:Enable", "false");
            x.Add("Vault:Address", "http://localhost:8200");
            x.Add("Vault:Token", "root");
            x.Add("Solution", "CodeDesignPlus");
            x.Add("AppName", "my-test");
            x.Add("RabbitMQ:UserName", "guest");
            x.Add("RabbitMQ:Password", "guest");
            x.Add("Security:ValidAudiences:0", Guid.NewGuid().ToString());
            x.Add("FileStorage:AzureFile:Enable", "false");
            x.Add("FileStorage:AzureBlob:Enable", "false");
            x.Add("FileStorage:Local:Enable", "true");
            x.Add("FileStorage:Local:Folder", Path.Combine(AppDomain.CurrentDomain.BaseDirectory));
        };
    }

    [Fact]
    public async Task GetFiles_ReturnOk()
    {
        var id = Guid.NewGuid();
        var tenant = Guid.NewGuid();

        await this.UploadFileAsync(id, tenant);

        var response = await this.RequestAsync($"http://localhost/api/FileStorage", null, HttpMethod.Get, tenant);

        var json = await response.Content.ReadAsStringAsync();

        var files = System.Text.Json.JsonSerializer.Deserialize<List<FileStorageDto>>(json, this.options)!;

        Assert.NotNull(files);
        Assert.NotEmpty(files);

        var file = files.FirstOrDefault(x => x.Id == id);

        Assert.NotNull(file);
        Assert.Equal(id, file.Id);
        Assert.True(file.Files[0].Success);
        Assert.Equal("fake.txt", file.Files[0].FileDetail.FullName);
        Assert.Equal("test", file.Files[0].FileDetail.Metadata.Target);

        Assert.True(Path.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tenant.ToString(), "test", "fake.txt")));
    }


    [Fact]
    public async Task UploadFile_ReturnOk()
    {

        var id = Guid.NewGuid();
        var tenant = Guid.NewGuid();

        await this.UploadFileAsync(id, tenant);

        var file = await this.GetRecordAsync(id, tenant);

        Assert.Equal(id, file.Id);
        Assert.True(file.Files[0].Success);
        Assert.Equal("fake.txt", file.Files[0].FileDetail.FullName);
        Assert.Equal("test", file.Files[0].FileDetail.Metadata.Target);

        Assert.True(Path.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tenant.ToString(), "test", "fake.txt")));
    }


    [Theory]
    [InlineData("attachment")]
    [InlineData("inline")]
    public async Task Download_ContentDisposition_Inline_Attachment(string contentDisposition)
    {
        var path = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(path, "fake.txt");
        var fileContentExpected = System.IO.File.ReadAllText(filePath);

        var id = Guid.NewGuid();
        var tenant = Guid.NewGuid();

        await this.UploadFileAsync(id, tenant);

        var responseDownload = await this.RequestAsync($"http://localhost/api/FileStorage/download/{id}?viewInBrowser={contentDisposition == "inline"}&file=fake.txt&target=test", null!, HttpMethod.Get, tenant);

        Assert.NotNull(responseDownload);
        Assert.Equal(HttpStatusCode.OK, responseDownload.StatusCode);

        Assert.Equal($"{contentDisposition}; filename=\"fake.txt\"", responseDownload.Content.Headers.ContentDisposition?.ToString());
        Assert.Equal("text/plain", responseDownload.Content.Headers.ContentType?.ToString());

        var contentStream = await responseDownload.Content.ReadAsStreamAsync();

        using var reader = new StreamReader(contentStream);
        var contentString = await reader.ReadToEndAsync();
        Assert.Equal(fileContentExpected, contentString);
    }

    private async Task<FileStorageDto> GetRecordAsync(Guid id, Guid tenant)
    {
        var response = await this.RequestAsync($"http://localhost/api/FileStorage/{id}", null, HttpMethod.Get, tenant);

        var json = await response.Content.ReadAsStringAsync();

        return System.Text.Json.JsonSerializer.Deserialize<FileStorageDto>(json, this.options)!;
    }

    private async Task<HttpResponseMessage> RequestAsync(string uri, HttpContent? content, HttpMethod method, Guid tenant)
    {
        var httpRequestMessage = new HttpRequestMessage()
        {
            RequestUri = new Uri(uri),
            Content = content,
            Method = method,
        };
        httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("TestAuth");
        httpRequestMessage.Headers.Add("X-Tenant", tenant.ToString());

        var response = await Client.SendAsync(httpRequestMessage);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(await response.Content.ReadAsStringAsync());
        }

        return response;
    }


    private async Task UploadFileAsync(Guid id, Guid tenant)
    {
        var path = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(path, "fake.txt");

        var stream = new MemoryStream(System.IO.File.ReadAllBytes(filePath))
        {
            Position = 0
        };

        var content = new MultipartFormDataContent
        {
            { new StringContent(id.ToString()), "id" },
            { new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("text/plain")
                    }
                }, "file", "fake.txt" },
            { new StringContent("test"), "target" },
            { new StringContent("false"), "renowned" }
        };

        var response = await this.RequestAsync("http://localhost/api/FileStorage/upload", content, HttpMethod.Post, tenant);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


}