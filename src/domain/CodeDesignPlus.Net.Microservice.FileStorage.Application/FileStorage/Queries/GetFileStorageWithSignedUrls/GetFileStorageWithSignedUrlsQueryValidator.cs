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
            .NotEqual(Guid.Empty);

        RuleFor(x => x.ExpirationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60);
    }
}
