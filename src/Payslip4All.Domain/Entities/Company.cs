namespace Payslip4All.Domain.Entities;
public class Company
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? UifNumber { get; set; }
    public string? SarsPayeNumber { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<Employee> Employees { get; set; } = new();
    
    public Company()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
