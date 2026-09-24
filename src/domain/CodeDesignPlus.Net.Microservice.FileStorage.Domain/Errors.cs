using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain;

public class Errors: IErrorCodes
{    
    public static readonly Error UnknownError = new("100", "UnknownError");
    public static readonly Error FileIsInvalid = new("101", "The file is invalid");
    public static readonly Error TargetIsInvalid = new("102", "The target is invalid");
    public static readonly Error UriIsInvalid = new("103", "The uri is invalid");
    public static readonly Error UriDownloadIsInvalid = new("104", "The uri download is invalid");
    public static readonly Error UriViewInBrowserIsInvalid = new("105", "The uri view in browser is invalid");

    public static readonly Error ExtensionIsInvalid = new("106", "The extension is invalid");
    public static readonly Error FullNameIsInvalid = new("107", "The full name is invalid"); 
    public static readonly Error MetadataIsInvalid = new("108", "The detail is invalid"); 
    public static readonly Error FileSizeIsInvalid = new("109", "The file size is invalid"); 
    public static readonly Error VersionIsInvalid = new("110", "The version is invalid");

    public static readonly Error FileDetailIsInvalid = new("111", "The file detail is invalid");
    public static readonly Error ProviderIsInvalid = new("112", "The provider is invalid");
    public static readonly Error FileAlreadyExists = new("113", "The file already exists");
    public static readonly Error FileDoesNotExists = new("114", "The file does not exists");
    public static readonly Error UpdateByIsInvalid = new("115", "The update by is invalid");
}
