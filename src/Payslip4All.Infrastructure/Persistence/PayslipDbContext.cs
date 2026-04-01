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
            e.Property(a => a.Description).HasMaxLength(300);
            e.Property(a => a.BalanceAfterActivity).HasPrecision(18, 2).IsRequired();
            e.Property(a => a.OccurredAt).IsRequired();
            e.HasIndex(a => new { a.WalletId, a.OccurredAt });
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
