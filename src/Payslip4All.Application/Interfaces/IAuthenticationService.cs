using Payslip4All.Application.DTOs.Auth;
namespace Payslip4All.Application.Interfaces;
public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterCommand command);
    Task<AuthResult> LoginAsync(LoginCommand command);
}
