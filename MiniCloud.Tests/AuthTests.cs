using Mini_Cloud.Application.Features.Auth;
using MiniCloud.Tests.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCloud.Tests;

public class AuthTests : TestBase
{
    private RegisterCommandHandler _registerCommand;
    private LoginCommandHandler _loginCommand;
    public AuthTests()
    {
        _registerCommand = new RegisterCommandHandler(DbContext, PasswordHasher);
        _loginCommand = new LoginCommandHandler(DbContext, PasswordHasher, JwtTokenGenerator);
    }
    [Fact]
    public async Task Register_ValidData_ShouldSucceed_And_PasswordMustBeHashed()
    {
        var command= new RegisterCommand("tester", "tester@example.com", "Password123!");
        var userId= await _registerCommand.Handle(command,CancellationToken.None);
        Assert.NotEqual(Guid.Empty, userId);
        var user = await DbContext.Users.FindAsync(userId);
        Assert.Equal("tester", user.UserName);
        Assert.Equal("tester@example.com",user.Gmail);
        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.True(PasswordHasher.VerifyPassword("Password123!", user.PasswordHash));
    }
}






