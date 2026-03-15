namespace Payslip4All.Domain.Entities;
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    
    public User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
