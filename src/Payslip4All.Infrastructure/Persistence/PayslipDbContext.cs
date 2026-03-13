using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence;

public class PayslipDbContext : DbContext
{
    public PayslipDbContext(DbContextOptions<PayslipDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Companies)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Company configuration
        modelBuilder.Entity<Company>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Company>()
            .HasMany(c => c.Employees)
            .WithOne(e => e.Company)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee configuration
        modelBuilder.Entity<Employee>()
            .HasKey(e => e.Id);
    }
}
