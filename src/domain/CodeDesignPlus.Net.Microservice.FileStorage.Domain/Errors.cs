namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain;

public class Errors: IErrorCodes
{    
    public const string UnknownError = "100 : UnknownError";
    public const string FileIsInvalid = "101 : The file is invalid";
    public const string TargetIsInvalid = "102 : The target is invalid";
    public const string UriIsInvalid = "103 : The uri is invalid";
    public const string UriDownloadIsInvalid = "104 : The uri download is invalid";
    public const string UriViewInBrowserIsInvalid = "105 : The uri view in browser is invalid";

    public const string ExtensionIsInvalid = "106 : The extension is invalid";
    public const string FullNameIsInvalid = "107 : The full name is invalid"; 
    public const string MetadataIsInvalid = "108 : The detail is invalid"; 
    public const string FileSizeIsInvalid = "109 : The file size is invalid"; 
    public const string VersionIsInvalid = "110 : The version is invalid";

    public const string FileDetailIsInvalid = "111 : The file detail is invalid";
    public const string ProviderIsInvalid = "112 : The provider is invalid";
    public const string FileAlreadyExists = "113 : The file already exists";
    public const string FileDoesNotExists = "114 : The file does not exists";
    public const string UpdateByIsInvalid = "115 : The update by is invalid";
}
