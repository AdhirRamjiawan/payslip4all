using System.Security.Cryptography;
using System.Text;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Infrastructure.Services;

public interface IAuthenticationService
{
    Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string email, string password, string? fullName = null);
    Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int userId);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly PayslipDbContext _dbContext;

    public AuthenticationService(PayslipDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(string username, string email, string password, string? fullName = null)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Username, email, and password are required", null);
        }

        // Check if user exists
        var existingUser = _dbContext.Users.FirstOrDefault(u => u.Username == username || u.Email == email);
        if (existingUser != null)
        {
            return (false, "Username or email already exists", null);
        }

        // Hash password
        var passwordHash = HashPassword(password);

        // Create user
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return (true, "User registered successfully", user);
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Username and password are required", null);
        }

        var user = _dbContext.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
        if (user == null)
        {
            return (false, "Invalid username or password", null);
        }

        // Verify password
        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Invalid username or password", null);
        }

        return (true, "Login successful", user);
    }

    public Task<User?> GetUserByIdAsync(int userId)
    {
        return Task.FromResult(_dbContext.Users.FirstOrDefault(u => u.Id == userId));
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }
}
