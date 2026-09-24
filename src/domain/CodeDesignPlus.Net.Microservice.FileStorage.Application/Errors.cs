using CodeDesignPlus.Net.Exceptions;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application;

public class Errors : IErrorCodes
{
    public static readonly Error UnknownError = new("200", "UnknownError");
    public static readonly Error InvalidRequest = new("201", "The request is invalid.");
    public static readonly Error FileStorageDoesNotExists = new("202", "The file storage does not exist.");

    public static readonly Error FileNotFound = new("203", "The file does not exist.");
}
