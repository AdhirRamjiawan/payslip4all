using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly Payslip4All.Application.Interfaces.IAuthenticationService _authService;

    public LoginModel(Payslip4All.Application.Interfaces.IAuthenticationService authService) => _authService = authService;

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var result = await _authService.LoginAsync(new Payslip4All.Application.DTOs.Auth.LoginCommand { Email = Email, Password = Password });
        if (!result.Success)
        {
            ErrorMessage = "Invalid email or password.";
            ReturnUrl = returnUrl;
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
        return LocalRedirect(returnUrl ?? "/");
    }
}
