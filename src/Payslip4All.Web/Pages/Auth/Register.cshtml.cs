using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly Payslip4All.Application.Interfaces.IAuthenticationService _authService;

    public RegisterModel(Payslip4All.Application.Interfaces.IAuthenticationService authService) => _authService = authService;

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _authService.RegisterAsync(new Payslip4All.Application.DTOs.Auth.RegisterCommand
        {
            Email = Email,
            Password = Password,
            ConfirmPassword = ConfirmPassword
        });

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? "Registration failed. Please try again.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId!.Value.ToString()),
            new(ClaimTypes.Email, result.UserEmail!),
            new(ClaimTypes.Name, result.UserEmail!),
            new(ClaimTypes.Role, "CompanyOwner")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Redirect("/");
    }
}
