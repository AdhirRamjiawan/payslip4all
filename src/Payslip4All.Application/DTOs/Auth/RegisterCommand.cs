namespace Payslip4All.Application.DTOs.Auth;
public class RegisterCommand
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
