using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTopUpAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletTopUpAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ConfirmedChargedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderSessionReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProviderPaymentReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReturnCorrelationToken = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FailureCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreditedWalletActivityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RedirectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastValidatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    HostedPageDeadline = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTopUpAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTopUpAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpAttempts_ProviderSessionReference",
                table: "WalletTopUpAttempts",
                column: "ProviderSessionReference");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpAttempts_UserId_CreatedAt",
                table: "WalletTopUpAttempts",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTopUpAttempts");
        }
    }
}
