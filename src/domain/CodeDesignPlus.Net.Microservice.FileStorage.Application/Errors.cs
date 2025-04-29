namespace CodeDesignPlus.Net.Microservice.FileStorage.Application;

public class Errors : IErrorCodes
{
    public const string UnknownError = "200 : UnknownError";
    public const string InvalidRequest = "201 : The request is invalid.";
    public const string FileStorageDoesNotExists = "202 : The file storage does not exist.";

    public const string FileNotFound = "203 : The file does not exist.";
}
