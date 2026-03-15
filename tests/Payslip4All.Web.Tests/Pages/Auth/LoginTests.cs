using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payslip4All.Application.DTOs.Auth;
using AppAuthService = Payslip4All.Application.Interfaces.IAuthenticationService;
using System.Security.Claims;

namespace Payslip4All.Web.Tests.Pages.Auth;

/// <summary>
/// xUnit tests for LoginModel (Razor Page).
/// T032 — TDD-first tests written before Login page implementation was locked.
/// Tests verify the success-redirect, generic-error, and claims-issuance behaviours.
/// </summary>
public class LoginTests
{
    private static LoginModel BuildModel(
        AppAuthService appAuthService,
        out Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService> aspAuthServiceMock)
    {
        // Mock the ASP.NET Core IAuthenticationService so SignInAsync doesn't throw.
        aspAuthServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        aspAuthServiceMock
            .Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(aspAuthServiceMock.Object);
        services.AddSingleton<IUrlHelperFactory>(new FakeUrlHelperFactory());
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        var model = new LoginModel(appAuthService);
        model.PageContext = new PageContext
        {
            HttpContext   = httpContext,
            RouteData     = new RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor()
        };

        return model;
    }

    [Fact]
    public async Task OnPostAsync_ValidCredentials_RedirectsToDashboard()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.LoginAsync(It.IsAny<LoginCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success   = true,
                UserId    = Guid.NewGuid(),
                UserEmail = "user@example.com"
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email    = "user@example.com";
        model.Password = "ValidP@ss1";

        var result = await model.OnPostAsync(returnUrl: null);

        Assert.IsType<LocalRedirectResult>(result);
        var redirect = (LocalRedirectResult)result;
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_ValidCredentialsWithReturnUrl_RedirectsToReturnUrl()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.LoginAsync(It.IsAny<LoginCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success   = true,
                UserId    = Guid.NewGuid(),
                UserEmail = "user@example.com"
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email    = "user@example.com";
        model.Password = "ValidP@ss1";

        var result = await model.OnPostAsync(returnUrl: "/companies");

        Assert.IsType<LocalRedirectResult>(result);
        var redirect = (LocalRedirectResult)result;
        Assert.Equal("/companies", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ReturnsPageWithGenericError()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.LoginAsync(It.IsAny<LoginCommand>()))
            .ReturnsAsync(new AuthResult { Success = false });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email    = "wrong@example.com";
        model.Password = "BadPassword";

        var result = await model.OnPostAsync(returnUrl: null);

        Assert.IsType<PageResult>(result);
        // Error message must never reveal whether the email or password was wrong (FR-004).
        Assert.Equal("Invalid email or password.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ErrorMessageIsGenericNotSpecific()
    {
        // FR-004: error messages must not specifically indicate whether the email
        // or the password was wrong (which would enable user enumeration).
        // "Invalid email or password." is the correct combined generic message.
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.LoginAsync(It.IsAny<LoginCommand>()))
            .ReturnsAsync(new AuthResult { Success = false });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email    = "nonexistent@example.com";
        model.Password = "AnyPassword";

        await model.OnPostAsync(null);

        // Must not say something that reveals whether ONLY the email was wrong
        // (e.g., "Email not found", "No account with that email").
        Assert.DoesNotContain("not found",    model.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email not",    model.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("incorrect password", model.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        // The error message must be non-empty so the user knows something went wrong.
        Assert.NotNull(model.ErrorMessage);
        Assert.NotEmpty(model.ErrorMessage!);
    }

    [Fact]
    public async Task OnPostAsync_ValidCredentials_IssuesCookieWithExpectedClaims()
    {
        var userId = Guid.NewGuid();
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.LoginAsync(It.IsAny<LoginCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success   = true,
                UserId    = userId,
                UserEmail = "claims@example.com"
            });

        ClaimsPrincipal? capturedPrincipal = null;
        var aspAuthMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        aspAuthMock
            .Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>(
                (_, _, principal, _) => capturedPrincipal = principal)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(aspAuthMock.Object);
        services.AddSingleton<IUrlHelperFactory>(new FakeUrlHelperFactory());
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var model = new LoginModel(mockAuth.Object);
        model.PageContext = new PageContext
        {
            HttpContext      = httpContext,
            RouteData        = new RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor()
        };
        model.Email    = "claims@example.com";
        model.Password = "ValidP@ss1";

        await model.OnPostAsync(null);

        Assert.NotNull(capturedPrincipal);
        Assert.Equal(userId.ToString(),
            capturedPrincipal!.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("CompanyOwner",
            capturedPrincipal.FindFirst(ClaimTypes.Role)?.Value);
    }
}

/// <summary>Stub IUrlHelperFactory — enough for LocalRedirect to function.</summary>
internal sealed class FakeUrlHelperFactory : IUrlHelperFactory
{
    public IUrlHelper GetUrlHelper(ActionContext context) => new FakeUrlHelper();
}

internal sealed class FakeUrlHelper : IUrlHelper
{
    public ActionContext ActionContext => new ActionContext();
    public string? Action(UrlActionContext actionContext)         => null;
    public string? Content(string? contentPath)                  => contentPath;
    public bool    IsLocalUrl(string? url)                       => url?.StartsWith("/") == true;
    public string? Link(string? routeName, object? values)       => null;
    public string? RouteUrl(UrlRouteContext routeContext)         => null;
}
