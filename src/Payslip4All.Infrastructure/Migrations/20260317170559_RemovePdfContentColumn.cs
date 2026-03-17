using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePdfContentColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfContent",
                table: "Payslips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PdfContent",
                table: "Payslips",
                type: "BLOB",
                nullable: true);
        }
    }
}
