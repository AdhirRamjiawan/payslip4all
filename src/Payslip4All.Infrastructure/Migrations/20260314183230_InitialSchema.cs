using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IdNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Occupation = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    UifReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MonthlyGrossSalary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TotalLoanAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NumberOfTerms = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyDeductionAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TermsCompleted = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayPeriodMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    PayPeriodYear = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossEarnings = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UifDeduction = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalLoanDeductions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PdfContent = table.Column<byte[]>(type: "BLOB", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payslips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayslipLoanDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayslipId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeLoanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipLoanDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipLoanDeductions_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayslipLoanDeductions_Payslips_PayslipId",
                        column: x => x.PayslipId,
                        principalTable: "Payslips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UserId",
                table: "Companies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_EmployeeId",
                table: "EmployeeLoans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_EmployeeId_Status",
                table: "EmployeeLoans",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber_CompanyId",
                table: "Employees",
                columns: new[] { "EmployeeNumber", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayslipLoanDeductions_EmployeeLoanId",
                table: "PayslipLoanDeductions",
                column: "EmployeeLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipLoanDeductions_PayslipId",
                table: "PayslipLoanDeductions",
                column: "PayslipId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId",
                table: "Payslips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear",
                table: "Payslips",
                columns: new[] { "EmployeeId", "PayPeriodMonth", "PayPeriodYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayslipLoanDeductions");

            migrationBuilder.DropTable(
                name: "EmployeeLoans");

            migrationBuilder.DropTable(
                name: "Payslips");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
