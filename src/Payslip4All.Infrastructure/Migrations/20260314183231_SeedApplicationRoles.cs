using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <summary>
    /// C1 compliance migration: creates the ApplicationRoles lookup table and seeds
    /// the CompanyOwner and SiteAdministrator role name constants.
    ///
    /// Purpose:
    ///   Constitution Principle IV requires the system to distinguish CompanyOwner from
    ///   SiteAdministrator at the data layer.  Feature 001 implements only CompanyOwner UI;
    ///   the SiteAdministrator admin portal is formally deferred to feature 002-admin-portal.
    ///   This migration satisfies the "MUST distinguish" requirement by seeding both role
    ///   strings into the DB now, so feature 002 can add admin pages without a schema change.
    ///
    /// See plan.md Complexity Tracking C1 for full rationale.
    /// </summary>
    public partial class SeedApplicationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the ApplicationRoles lookup table.
            // This table is intentionally outside the EF Core model so that
            // future model-diff migrations do not attempt to alter or drop it.
            migrationBuilder.CreateTable(
                name: "ApplicationRoles",
                columns: table => new
                {
                    Id   = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRoles", x => x.Id);
                });

            // Seed the two canonical role names.
            // Uses raw SQL because ApplicationRoles is intentionally outside the EF model
            // (migrationBuilder.InsertData requires the table to have a mapped entity type).
            // Raw SQL is provider-neutral for simple INSERT statements.
            migrationBuilder.Sql(
                "INSERT INTO ApplicationRoles (Id, Name) VALUES ('1', 'CompanyOwner');");
            migrationBuilder.Sql(
                "INSERT INTO ApplicationRoles (Id, Name) VALUES ('2', 'SiteAdministrator');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApplicationRoles");
        }
    }
}
