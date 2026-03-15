namespace Payslip4All.Domain.Constants;

/// <summary>
/// Authoritative string constants for application roles.
/// All layers reference these values — never use magic strings.
/// SiteAdministrator is seeded in the database (C4 deviation) and
/// will be enforced in feature 002-admin-portal.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// Employer / company-owner role. Granted on registration and on every login.
    /// All Blazor pages and the PDF download endpoint enforce this role.
    /// </summary>
    public const string CompanyOwner = "CompanyOwner";

    /// <summary>
    /// Platform administrator role. Formally deferred to feature 002-admin-portal (C4 deviation).
    /// Seeded into the ApplicationRoles lookup table in this feature so the string
    /// exists in the DB before the admin portal ships.
    /// </summary>
    public const string SiteAdministrator = "SiteAdministrator";
}
