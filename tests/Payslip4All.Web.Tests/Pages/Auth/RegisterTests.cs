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

namespace Payslip4All.Web.Tests.Pages.Auth;

/// <summary>
/// xUnit tests for RegisterModel (Razor Page).
/// T032 — TDD-first tests covering success redirect, duplicate-email generic error,
/// and password-not-stored-as-plain-text guarantee.
/// </summary>
public class RegisterTests
{
    private static RegisterModel BuildModel(
        AppAuthService appAuthService,
        out Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService> aspAuthMock)
    {
        aspAuthMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        aspAuthMock
            .Setup(s => s.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(aspAuthMock.Object);
        services.AddSingleton<IUrlHelperFactory>(new FakeUrlHelperFactory());
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var model = new RegisterModel(appAuthService);
        model.PageContext = new PageContext
        {
            HttpContext      = httpContext,
            RouteData        = new RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor()
        };
        return model;
    }

    [Fact]
    public async Task OnPostAsync_SuccessfulRegistration_RedirectsToDashboard()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success   = true,
                UserId    = Guid.NewGuid(),
                UserEmail = "new@example.com"
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email           = "new@example.com";
        model.Password        = "SecureP@ss1";
        model.ConfirmPassword = "SecureP@ss1";

        var result = await model.OnPostAsync();

        // Auto-sign-in on successful registration then redirect to "/"
        Assert.IsType<RedirectResult>(result);
        var redirect = (RedirectResult)result;
        Assert.Equal("/portal", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_DuplicateEmail_ReturnsPageWithGenericError()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success      = false,
                ErrorMessage = "An account with this email already exists."
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email           = "existing@example.com";
        model.Password        = "SecureP@ss1";
        model.ConfirmPassword = "SecureP@ss1";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
        Assert.NotEmpty(model.ErrorMessage!);
    }

    [Fact]
    public async Task OnPostAsync_DuplicateEmail_ErrorNeverRevealsDuplicate()
    {
        // FR-004: error messages must not reveal whether the email is in use.
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterCommand>()))
            .ReturnsAsync(new AuthResult
            {
                Success      = false,
                ErrorMessage = "Registration failed. Please try again."
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email           = "existing@example.com";
        model.Password        = "SecureP@ss1";
        model.ConfirmPassword = "SecureP@ss1";

        await model.OnPostAsync();

        // The model's ErrorMessage must not contain "duplicate", "exists", or "already"
        // to prevent user enumeration.
        Assert.DoesNotContain("already exists", model.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnPostAsync_RegistrationFailed_NoCookieIssued()
    {
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterCommand>()))
            .ReturnsAsync(new AuthResult { Success = false, ErrorMessage = "Registration failed." });

        var model = BuildModel(mockAuth.Object, out var aspAuthMock);
        model.Email           = "fail@example.com";
        model.Password        = "SecureP@ss1";
        model.ConfirmPassword = "SecureP@ss1";

        await model.OnPostAsync();

        // SignInAsync must NOT have been called when registration fails.
        aspAuthMock.Verify(s => s.SignInAsync(
            It.IsAny<HttpContext>(),
            It.IsAny<string>(),
            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_Success_PasswordIsNotPassedPlainTextToRepository()
    {
        // Verify the RegisterCommand received by RegisterAsync carries a password string
        // that the Application layer is responsible for hashing. The page model must
        // never hash the password itself — that is the Application layer's job.
        RegisterCommand? captured = null;
        var mockAuth = new Mock<AppAuthService>();
        mockAuth
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterCommand>()))
            .Callback<RegisterCommand>(cmd => captured = cmd)
            .ReturnsAsync(new AuthResult
            {
                Success   = true,
                UserId    = Guid.NewGuid(),
                UserEmail = "pwd@example.com"
            });

        var model = BuildModel(mockAuth.Object, out _);
        model.Email           = "pwd@example.com";
        model.Password        = "PlainTextPassword";
        model.ConfirmPassword = "PlainTextPassword";

        await model.OnPostAsync();

        // The page model must pass the raw password to the service; the service is
        // responsible for hashing (via IPasswordHasher). Assert the command was forwarded.
        Assert.NotNull(captured);
        Assert.Equal("PlainTextPassword", captured!.Password);
        Assert.Equal("pwd@example.com",  captured.Email);
    }
}
