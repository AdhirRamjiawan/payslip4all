using Moq;
using Payslip4All.Application.DTOs.Auth;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Application.Services;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _service = new AuthenticationService(_mockUserRepo.Object, _mockHasher.Object);
    }

    [Fact]
    public async Task RegisterAsync_UniqueEmail_ReturnsSuccess()
    {
        _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(new RegisterCommand
        {
            Email = "test@test.com",
            Password = "password",
            ConfirmPassword = "password"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.UserId);
        Assert.Equal("test@test.com", result.UserEmail);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsGenericError()
    {
        _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var result = await _service.RegisterAsync(new RegisterCommand
        {
            Email = "test@test.com",
            Password = "pass",
            ConfirmPassword = "pass"
        });

        Assert.False(result.Success);
        // Generic error - must NOT reveal that email already exists
        Assert.DoesNotContain("exists", result.ErrorMessage?.ToLower() ?? "");
        Assert.DoesNotContain("already", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task RegisterAsync_MismatchedPasswords_ReturnsFailure()
    {
        var result = await _service.RegisterAsync(new RegisterCommand
        {
            Email = "test@test.com",
            Password = "password1",
            ConfirmPassword = "password2"
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsSuccess()
    {
        var user = new User { Email = "test@test.com" };
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _service.LoginAsync(new LoginCommand
        {
            Email = "test@test.com",
            Password = "password"
        });

        Assert.True(result.Success);
        Assert.Equal("test@test.com", result.UserEmail);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsGenericError()
    {
        var user = new User { Email = "test@test.com" };
        _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await _service.LoginAsync(new LoginCommand
        {
            Email = "test@test.com",
            Password = "wrong"
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_StoredHashIsNotPlaintext()
    {
        string? capturedHash = null;
        _mockUserRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockHasher.Setup(h => h.Hash("mypassword")).Returns("bcrypt-hash-value");
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedHash = u.PasswordHash)
            .Returns(Task.CompletedTask);

        await _service.RegisterAsync(new RegisterCommand
        {
            Email = "t@t.com",
            Password = "mypassword",
            ConfirmPassword = "mypassword"
        });

        Assert.NotEqual("mypassword", capturedHash);
        Assert.Equal("bcrypt-hash-value", capturedHash);
    }
}
