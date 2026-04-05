using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayFastCardIntegrationAuditParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AbandonedAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiredAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReconciledAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantPaymentReference",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextReconciliationDueAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentReturnEvidenceId",
                table: "WalletActivities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedCurrencyCode",
                table: "PaymentReturnEvidences",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentMode",
                table: "PaymentReturnEvidences",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantPaymentReference",
                table: "PaymentReturnEvidences",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "PaymentReturnEvidences",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodCode",
                table: "PaymentReturnEvidences",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ServerConfirmed",
                table: "PaymentReturnEvidences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SignatureVerified",
                table: "PaymentReturnEvidences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SourceVerified",
                table: "PaymentReturnEvidences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupersededNotConfirmed",
                table: "OutcomeNormalizationDecisions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TriggerSource",
                table: "OutcomeNormalizationDecisions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpAttempts_MerchantPaymentReference",
                table: "WalletTopUpAttempts",
                column: "MerchantPaymentReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTopUpAttempts_MerchantPaymentReference",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "AbandonedAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "LastReconciledAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "MerchantPaymentReference",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "NextReconciliationDueAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "PaymentReturnEvidenceId",
                table: "WalletActivities");

            migrationBuilder.DropColumn(
                name: "ConfirmedCurrencyCode",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "EnvironmentMode",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "MerchantPaymentReference",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "PaymentMethodCode",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "ServerConfirmed",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "SignatureVerified",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "SourceVerified",
                table: "PaymentReturnEvidences");

            migrationBuilder.DropColumn(
                name: "SupersededNotConfirmed",
                table: "OutcomeNormalizationDecisions");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "OutcomeNormalizationDecisions");
        }
    }
}
