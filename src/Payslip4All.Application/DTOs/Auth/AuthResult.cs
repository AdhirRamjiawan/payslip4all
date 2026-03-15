namespace Payslip4All.Application.DTOs.Auth;
public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
}
