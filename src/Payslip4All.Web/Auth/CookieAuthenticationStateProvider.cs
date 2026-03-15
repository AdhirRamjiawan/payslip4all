using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Payslip4All.Web.Auth;

/// <summary>
/// Bridges ASP.NET Core cookie authentication into the Blazor component tree.
/// Reads the current <see cref="ClaimsPrincipal"/> from <see cref="IHttpContextAccessor"/>
/// and exposes it as the authentication state for all Blazor components.
///
/// Rationale for placement in Web (not Infrastructure):
///   This class takes a hard dependency on IHttpContextAccessor — an ASP.NET Core Web
///   host abstraction. Infrastructure must not depend on web-host types. The Web layer
///   is the only correct home.
///
/// LX note: this is the concrete realisation of what the constitution calls
///   "BlazorAuthenticationStateProvider". The two names refer to the same component.
/// </summary>
public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// Returns the current authentication state derived from the HTTP request cookie.
    /// Returns an anonymous (unauthenticated) identity when no HttpContext is available
    /// (e.g. during pre-rendering or SignalR reconnection before context is established).
    /// </summary>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User
                   ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }

    /// <summary>
    /// Notifies the Blazor component tree that the authentication state has changed.
    /// Call this after issuing or revoking a cookie so Blazor re-evaluates [Authorize].
    /// </summary>
    public void NotifyAuthenticationStateChanged(ClaimsPrincipal user)
        => NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));

    /// <summary>
    /// Extracts the authenticated user's ID from the NameIdentifier claim.
    /// Returns <see langword="null"/> if the user is unauthenticated or the claim is absent/malformed.
    /// </summary>
    public Guid? GetAuthenticatedUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User
                        .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
