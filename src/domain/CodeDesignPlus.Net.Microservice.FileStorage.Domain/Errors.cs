using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Domain;

public class Errors: IErrorCodes
{    
    public static readonly Error UnknownError = new("100");
    public static readonly Error FileIsInvalid = new("101");
    public static readonly Error TargetIsInvalid = new("102");
    public static readonly Error UriIsInvalid = new("103");
    public static readonly Error UriDownloadIsInvalid = new("104");
    public static readonly Error UriViewInBrowserIsInvalid = new("105");

    public static readonly Error ExtensionIsInvalid = new("106");
    public static readonly Error FullNameIsInvalid = new("107"); 
    public static readonly Error MetadataIsInvalid = new("108"); 
    public static readonly Error FileSizeIsInvalid = new("109"); 
    public static readonly Error VersionIsInvalid = new("110");

    public static readonly Error FileDetailIsInvalid = new("111");
    public static readonly Error ProviderIsInvalid = new("112");
    public static readonly Error FileAlreadyExists = new("113");
    public static readonly Error FileDoesNotExists = new("114");
    public static readonly Error UpdateByIsInvalid = new("115");
}
