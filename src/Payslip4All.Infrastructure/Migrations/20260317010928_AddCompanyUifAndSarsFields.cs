using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyUifAndSarsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SarsPayeNumber",
                table: "Companies",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UifNumber",
                table: "Companies",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SarsPayeNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "UifNumber",
                table: "Companies");
        }
    }
}
