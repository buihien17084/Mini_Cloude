using MediatR;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Features.Auth
{
    public record LoginCommand(
    string Gmail,
    string Password
) : IRequest<LoginResponse>;

    public record LoginResponse(
        string Token,
        UserDto User
    );

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private static string? _dummyHash;

        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var email = request.Gmail?.Trim().ToLowerInvariant() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Gmail == email, cancellationToken);

            const string invalidCredentialsMsg = "Email hoặc mật khẩu không chính xác.";

            if (user == null)
            {
                _passwordHasher.VerifyPassword(password, (_dummyHash ??= _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"))));
                throw new UnauthorizedException(invalidCredentialsMsg);
            }

            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                throw new UnauthorizedException(invalidCredentialsMsg);
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new LoginResponse(
                token,
                new UserDto(user.Id, user.UserName, user.Gmail)
            );
        }
    }
}
