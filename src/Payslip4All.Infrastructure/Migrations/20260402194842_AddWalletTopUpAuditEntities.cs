using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payslip4All.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTopUpAuditEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AbandonAfterUtc",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "AuthoritativeEvidenceId",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AuthoritativeOutcomeAcceptedAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEvaluatedAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEvidenceReceivedAt",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeMessage",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeReasonCode",
                table: "WalletTopUpAttempts",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutcomeNormalizationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentReturnEvidenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UnmatchedPaymentReturnRecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecisionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AppliedPrecedence = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedOutcome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AuthoritativeOutcomeBefore = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AuthoritativeOutcomeAfter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DecisionReasonCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DecisionSummary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SupersededAbandonment = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConflictWithAcceptedFinalOutcome = table.Column<bool>(type: "INTEGER", nullable: false),
                    WalletEffect = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WalletActivityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeNormalizationDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReturnEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceChannel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderSessionReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProviderPaymentReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReturnCorrelationToken = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MatchedAttemptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CorrelationDisposition = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimedOutcome = table.Column<int>(type: "INTEGER", nullable: true),
                    TrustLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfirmedChargedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    EvidenceOccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsAtOrAfterAbandonmentThreshold = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafePayloadSnapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ValidationMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReturnEvidences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnmatchedPaymentReturnRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrimaryEvidenceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CorrelationDisposition = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GenericResultCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SafePayloadSnapshot = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnmatchedPaymentReturnRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpAttempts_ReturnCorrelationToken",
                table: "WalletTopUpAttempts",
                column: "ReturnCorrelationToken");

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeNormalizationDecisions_AttemptId",
                table: "OutcomeNormalizationDecisions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReturnEvidences_MatchedAttemptId",
                table: "PaymentReturnEvidences",
                column: "MatchedAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutcomeNormalizationDecisions");

            migrationBuilder.DropTable(
                name: "PaymentReturnEvidences");

            migrationBuilder.DropTable(
                name: "UnmatchedPaymentReturnRecords");

            migrationBuilder.DropIndex(
                name: "IX_WalletTopUpAttempts_ReturnCorrelationToken",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "AbandonAfterUtc",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "AuthoritativeEvidenceId",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "AuthoritativeOutcomeAcceptedAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "LastEvidenceReceivedAt",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "OutcomeMessage",
                table: "WalletTopUpAttempts");

            migrationBuilder.DropColumn(
                name: "OutcomeReasonCode",
                table: "WalletTopUpAttempts");
        }
    }
}
