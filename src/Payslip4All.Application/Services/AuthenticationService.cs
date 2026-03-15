using Payslip4All.Application.DTOs.Auth;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Services;
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    public AuthenticationService(IUserRepository userRepo, IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _hasher = hasher;
    }
    public async Task<AuthResult> RegisterAsync(RegisterCommand command)
    {
        if (command.Password != command.ConfirmPassword)
            return new AuthResult { Success = false, ErrorMessage = "Passwords do not match." };
        var email = command.Email.ToLower();
        if (await _userRepo.ExistsAsync(email))
            return new AuthResult { Success = false, ErrorMessage = "Registration failed." };
        var user = new User { Email = email, PasswordHash = _hasher.Hash(command.Password) };
        await _userRepo.AddAsync(user);
        return new AuthResult { Success = true, UserId = user.Id, UserEmail = user.Email };
    }
    public async Task<AuthResult> LoginAsync(LoginCommand command)
    {
        var user = await _userRepo.GetByEmailAsync(command.Email.ToLower());
        if (user == null || !_hasher.Verify(command.Password, user.PasswordHash))
            return new AuthResult { Success = false, ErrorMessage = "Invalid email or password." };
        return new AuthResult { Success = true, UserId = user.Id, UserEmail = user.Email };
    }
}
