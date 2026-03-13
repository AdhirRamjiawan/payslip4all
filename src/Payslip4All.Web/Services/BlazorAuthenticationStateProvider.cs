using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Web.Services;

public class BlazorAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PayslipDbContext _dbContext;

    public BlazorAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, PayslipDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated ?? false)
        {
            var principal = httpContext.User;
            return new AuthenticationState(principal);
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task LoginAsync(User user)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "BlazorAuth");
            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await httpContext.SignInAsync(
                "BlazorAuth",
                new ClaimsPrincipal(claimsIdentity),
                authenticationProperties);

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync("BlazorAuth");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
