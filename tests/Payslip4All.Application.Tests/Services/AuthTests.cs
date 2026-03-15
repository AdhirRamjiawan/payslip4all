using Moq;
using Payslip4All.Application.DTOs.Auth;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class AuthTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly AuthenticationService _service;

    public AuthTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _service = new AuthenticationService(_mockUserRepo.Object, _mockHasher.Object);
    }

    [Fact]
    public async Task RegisterAsync_EmailIsNormalisedToLowercase()
    {
        string? capturedEmail = null;
        _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .Callback<string>(e => capturedEmail = e)
            .ReturnsAsync(false);
        _mockHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        await _service.RegisterAsync(new RegisterCommand
        {
            Email = "Test@Example.COM",
            Password = "pass",
            ConfirmPassword = "pass"
        });

        Assert.Equal("test@example.com", capturedEmail);
    }

    [Fact]
    public async Task LoginAsync_EmailIsNormalisedToLowercase()
    {
        string? capturedEmail = null;
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .Callback<string>(e => capturedEmail = e)
            .ReturnsAsync((User?)null);

        await _service.LoginAsync(new LoginCommand
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "password"
        });

        Assert.Equal("test@example.com", capturedEmail);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsGenericError()
    {
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(new LoginCommand
        {
            Email = "unknown@test.com",
            Password = "password"
        });

        Assert.False(result.Success);
        // Generic error - must not reveal that user doesn't exist
        Assert.DoesNotContain("not found", result.ErrorMessage?.ToLower() ?? "");
        Assert.DoesNotContain("no user", result.ErrorMessage?.ToLower() ?? "");
    }
}
