using CodeDesignPlus.Net.Microservice.FileStorage.Application.FileStorage;
using Xunit;

namespace CodeDesignPlus.Net.Microservice.FileStorage.Application.Test.FileStorage;

/// <summary>
/// Every target an upload screen of Kappali.Frontend sends has to be allowed here, or the upload fails with
/// <c>TargetIsNotAllowed</c> (pendings/293: the cash deposit support shipped with a target nobody had added).
/// </summary>
public class FileScopeTest
{
    /// <summary>
    /// The targets used by the frontend on 2026-10-05 (<c>target="…"</c> and <c>target: '…'</c> under app/).
    /// </summary>
    [Theory]
    [InlineData("cash-deposits")]
    [InlineData("common-areas")]
    [InlineData("email-templates")]
    [InlineData("expense-invoices")]
    [InlineData("fee-exclusions")]
    [InlineData("infraction-appeal")]
    [InlineData("infraction-evidence")]
    [InlineData("lease-contracts")]
    [InlineData("moving-inspections")]
    [InlineData("ownership-proofs")]
    [InlineData("pqrs-attachments")]
    [InlineData("quotation-documents")]
    [InlineData("system-email-templates")]
    [InlineData("users")]
    [InlineData("vehicle-documents")]
    public void AllowedTargets_IncludesEveryTargetTheScreensUpload(string target)
    {
        Assert.Contains(target, FileScope.AllowedTargets);
    }
}
