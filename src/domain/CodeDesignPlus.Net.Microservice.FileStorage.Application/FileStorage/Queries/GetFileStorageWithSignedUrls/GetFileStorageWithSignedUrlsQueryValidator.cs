namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage.Queries.GetFileStorageWithSignedUrls;

/// <summary>
/// Validator for GetFileStorageWithSignedUrlsQuery
/// </summary>
public class GetFileStorageWithSignedUrlsQueryValidator : AbstractValidator<GetFileStorageWithSignedUrlsQuery>
{
    public GetFileStorageWithSignedUrlsQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Id must be a valid GUID");

        RuleFor(x => x.ExpirationMinutes)
            .GreaterThan(0)
            .WithMessage("ExpirationMinutes must be greater than 0")
            .LessThanOrEqualTo(60)
            .WithMessage("ExpirationMinutes must not exceed 60 minutes");
    }
}
