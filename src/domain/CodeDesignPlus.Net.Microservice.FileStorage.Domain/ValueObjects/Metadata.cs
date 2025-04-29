using System.Text.Json.Serialization;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain.ValueObjects;

public sealed partial class Metadata
{
    public string File { get; private set; } = null!;
    public Uri Uri { get; private set; } = null!;
    public string Target { get; private set; } = null!;
    public string UriDownload { get; private set; } = null!;
    public string UriViewInBrowser { get; private set; } = null!;
    public string Provider { get; private set; } = null!;

    public Metadata()
    {

    }

    [JsonConstructor]
    public Metadata(string file, string target, Uri uri, string uriDownload, string uriViewInBrowser, string provider)
    {
        DomainGuard.IsNullOrEmpty(file, Errors.FileIsInvalid);
        DomainGuard.IsNullOrEmpty(target, Errors.TargetIsInvalid);
        DomainGuard.IsNull(uri, Errors.UriIsInvalid);
        DomainGuard.IsNullOrEmpty(uriDownload, Errors.UriDownloadIsInvalid);
        DomainGuard.IsNullOrEmpty(uriViewInBrowser, Errors.UriViewInBrowserIsInvalid);

        this.File = file;
        this.Target = target;
        this.Uri = uri;
        this.UriDownload = uriDownload;
        this.UriViewInBrowser = uriViewInBrowser;
    }

    public static Metadata Create(string file, string target, Uri uri, string uriDownload, string uriViewInBrowser, string provider)
    {
        return new Metadata(file, target, uri, uriDownload, uriViewInBrowser, provider);
    }
}
