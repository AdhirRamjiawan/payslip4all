using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;
namespace Payslip4All.Infrastructure.Persistence;
public class PayslipDbContext : DbContext, IUnitOfWork
{
    public PayslipDbContext(DbContextOptions<PayslipDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<PayslipLoanDeduction> PayslipLoanDeductions => Set<PayslipLoanDeduction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletActivity> WalletActivities => Set<WalletActivity>();
    public DbSet<WalletTopUpAttempt> WalletTopUpAttempts => Set<WalletTopUpAttempt>();
    public DbSet<PaymentReturnEvidence> PaymentReturnEvidences => Set<PaymentReturnEvidence>();
    public DbSet<OutcomeNormalizationDecision> OutcomeNormalizationDecisions => Set<OutcomeNormalizationDecision>();
    public DbSet<UnmatchedPaymentReturnRecord> UnmatchedPaymentReturnRecords => Set<UnmatchedPaymentReturnRecord>();
    public DbSet<PayslipPricingSetting> PayslipPricingSettings => Set<PayslipPricingSetting>();
    
    private IDbContextTransaction? _currentTransaction;
    
    public async Task BeginTransactionAsync()
    {
        _currentTransaction = await Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(60).IsRequired();
            e.Property(u => u.CreatedAt).IsRequired();
        });
        
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.UifNumber).HasMaxLength(50);
            e.Property(c => c.SarsPayeNumber).HasMaxLength(30);
            e.HasOne<User>()
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => c.UserId);
            e.HasMany(c => c.Employees)
             .WithOne(emp => emp.Company)
             .HasForeignKey(emp => emp.CompanyId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(emp => emp.Id);
            e.Property(emp => emp.FirstName).HasMaxLength(100).IsRequired();
            e.Property(emp => emp.LastName).HasMaxLength(100).IsRequired();
            e.Property(emp => emp.IdNumber).HasMaxLength(20).IsRequired();
            e.Property(emp => emp.EmployeeNumber).HasMaxLength(50).IsRequired();
            e.Property(emp => emp.Occupation).HasMaxLength(150).IsRequired();
            e.Property(emp => emp.UifReference).HasMaxLength(50);
            e.Property(emp => emp.MonthlyGrossSalary).HasPrecision(18, 2).IsRequired();
            e.HasIndex(emp => emp.CompanyId);
            e.HasIndex(emp => new { emp.EmployeeNumber, emp.CompanyId }).IsUnique();
            e.HasMany(emp => emp.Loans)
             .WithOne(l => l.Employee)
             .HasForeignKey(l => l.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(emp => emp.Payslips)
             .WithOne(p => p.Employee)
             .HasForeignKey(p => p.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<EmployeeLoan>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Description).HasMaxLength(300).IsRequired();
            e.Property(l => l.TotalLoanAmount).HasPrecision(18, 2).IsRequired();
            e.Property(l => l.MonthlyDeductionAmount).HasPrecision(18, 2).IsRequired();
            e.Property(l => l.TermsCompleted).IsRequired().HasDefaultValue(0).IsConcurrencyToken();
            e.Property(l => l.Status).IsRequired().HasConversion<int>();
            e.HasIndex(l => l.EmployeeId);
            e.HasIndex(l => new { l.EmployeeId, l.Status });
        });
        
        modelBuilder.Entity<Payslip>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.GrossEarnings).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.UifDeduction).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.TotalLoanDeductions).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.TotalDeductions).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.NetPay).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.ChargedAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
            e.HasIndex(p => p.EmployeeId);
            e.HasIndex(p => new { p.EmployeeId, p.PayPeriodMonth, p.PayPeriodYear })
             .IsUnique()
             .HasDatabaseName("UQ_Payslips_EmployeeId_PayPeriodMonth_PayPeriodYear");
            e.HasMany(p => p.LoanDeductions)
             .WithOne()
             .HasForeignKey(d => d.PayslipId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<PayslipLoanDeduction>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Description).HasMaxLength(300).IsRequired();
            e.Property(d => d.Amount).HasPrecision(18, 2).IsRequired();
            e.HasOne<EmployeeLoan>()
             .WithMany()
             .HasForeignKey(d => d.EmployeeLoanId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.PayslipId);
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.CurrentBalance).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
            e.Property(w => w.CreatedAt).IsRequired();
            e.Property(w => w.UpdatedAt).IsRequired().IsConcurrencyToken();
            e.HasIndex(w => w.UserId).IsUnique();
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.Activities)
                .WithOne(a => a.Wallet)
                .HasForeignKey(a => a.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WalletActivity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.ActivityType).HasConversion<int>().IsRequired();
            e.Property(a => a.Amount).HasPrecision(18, 2).IsRequired();
            e.Property(a => a.ReferenceType).HasMaxLength(100);
            e.Property(a => a.ReferenceId).HasMaxLength(100);
            e.Property(a => a.PaymentReturnEvidenceId);
            e.Property(a => a.Description).HasMaxLength(300);
            e.Property(a => a.BalanceAfterActivity).HasPrecision(18, 2).IsRequired();
            e.Property(a => a.OccurredAt).IsRequired();
            e.HasIndex(a => new { a.WalletId, a.OccurredAt });
        });

        modelBuilder.Entity<WalletTopUpAttempt>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.RequestedAmount).HasPrecision(18, 2).IsRequired();
            e.Property(a => a.ConfirmedChargedAmount).HasPrecision(18, 2);
            e.Property(a => a.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(a => a.Status).HasConversion<int>().IsRequired();
            e.Property(a => a.ProviderKey).HasMaxLength(100).IsRequired();
            e.Property(a => a.ProviderSessionReference).HasMaxLength(200);
            e.Property(a => a.ProviderPaymentReference).HasMaxLength(200);
            e.Property(a => a.MerchantPaymentReference).HasMaxLength(200).IsRequired();
            e.Property(a => a.ReturnCorrelationToken).HasMaxLength(200);
            e.Property(a => a.FailureCode).HasMaxLength(100);
            e.Property(a => a.FailureMessage).HasMaxLength(300);
            e.Property(a => a.OutcomeReasonCode).HasMaxLength(100);
            e.Property(a => a.OutcomeMessage).HasMaxLength(300);
            e.Property(a => a.CreatedAt).IsRequired();
            e.Property(a => a.UpdatedAt).IsRequired();
            e.Property(a => a.AbandonAfterUtc).IsRequired();
            e.Property(a => a.LastReconciledAt);
            e.Property(a => a.CancelledAt);
            e.Property(a => a.ExpiredAt);
            e.Property(a => a.AbandonedAt);
            e.Property(a => a.NextReconciliationDueAt);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
            e.HasIndex(a => a.MerchantPaymentReference);
            e.HasIndex(a => a.ProviderSessionReference);
            e.HasIndex(a => a.ReturnCorrelationToken);
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentReturnEvidence>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProviderKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.SourceChannel).HasMaxLength(50).IsRequired();
            e.Property(x => x.ProviderSessionReference).HasMaxLength(200);
            e.Property(x => x.ProviderPaymentReference).HasMaxLength(200);
            e.Property(x => x.MerchantPaymentReference).HasMaxLength(200);
            e.Property(x => x.ReturnCorrelationToken).HasMaxLength(200);
            e.Property(x => x.CorrelationDisposition).HasConversion<int>().IsRequired();
            e.Property(x => x.ClaimedOutcome).HasConversion<int?>();
            e.Property(x => x.TrustLevel).HasConversion<int>().IsRequired();
            e.Property(x => x.PaymentMethodCode).HasMaxLength(50);
            e.Property(x => x.EnvironmentMode).HasMaxLength(20);
            e.Property(x => x.ConfirmedCurrencyCode).HasMaxLength(3);
            e.Property(x => x.ConfirmedChargedAmount).HasPrecision(18, 2);
            e.Property(x => x.SafePayloadSnapshot).HasMaxLength(2000);
            e.Property(x => x.ValidationMessage).HasMaxLength(500);
            e.HasIndex(x => x.MatchedAttemptId);
        });

        modelBuilder.Entity<OutcomeNormalizationDecision>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DecisionType).HasMaxLength(100).IsRequired();
            e.Property(x => x.TriggerSource).HasMaxLength(100).IsRequired();
            e.Property(x => x.AppliedPrecedence).HasMaxLength(100).IsRequired();
            e.Property(x => x.NormalizedOutcome).HasMaxLength(100).IsRequired();
            e.Property(x => x.AuthoritativeOutcomeBefore).HasMaxLength(100);
            e.Property(x => x.AuthoritativeOutcomeAfter).HasMaxLength(100);
            e.Property(x => x.DecisionReasonCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.DecisionSummary).HasMaxLength(500).IsRequired();
            e.Property(x => x.WalletEffect).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.AttemptId);
        });

        modelBuilder.Entity<UnmatchedPaymentReturnRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProviderKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.CorrelationDisposition).HasMaxLength(100).IsRequired();
            e.Property(x => x.GenericResultCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayMessage).HasMaxLength(500).IsRequired();
            e.Property(x => x.SafePayloadSnapshot).HasMaxLength(2000);
        });

        modelBuilder.Entity<PayslipPricingSetting>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.PricePerPayslip).HasPrecision(18, 2).IsRequired();
            e.Property(p => p.UpdatedByUserId).HasMaxLength(100);
            e.Property(p => p.UpdatedAt).IsRequired();
            e.HasData(new
            {
                Id = PayslipPricingSetting.DefaultId,
                PricePerPayslip = PayslipPricingSetting.DefaultPricePerPayslip,
                UpdatedByUserId = (string?)null,
                UpdatedAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            });
        });
    }
}
