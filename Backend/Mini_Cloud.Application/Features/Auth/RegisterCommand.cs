using Mini_Cloud.Application.Common.Interfaces;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Domain.Entities;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;

namespace Mini_Cloud.Application.Features.Auth
{
    public record RegisterCommand(
     string UserName,
     string Gmail,
     string Password
 ) : IRequest<Guid>;

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var userName = request.UserName?.Trim() ?? string.Empty;
            var email = request.Gmail?.Trim().ToLowerInvariant() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            // Validation rules
            if (userName.Length < 3 || userName.Length > 50)
            {
                throw new BadRequestException("Tên đăng nhập phải có từ 3 đến 50 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(email) || email.Length > 255 || !EmailRegex.IsMatch(email))
            {
                throw new BadRequestException("Định dạng email không hợp lệ hoặc vượt quá 255 ký tự.");
            }

            if (password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72)
            {
                throw new BadRequestException("Mật khẩu phải từ 8 ký tự và không quá 72 byte (khoảng 72 ký tự không dấu).");
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Gmail == email, cancellationToken);

            if (emailExists)
            {
                throw new ConflictException("Email đã được sử dụng.");
            }

            var passwordHash = _passwordHasher.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Gmail = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.Users.Remove(user);
                var nowExists = await _context.Users
                    .AnyAsync(u => u.Gmail == email, cancellationToken);
                if (nowExists)
                {
                    throw new ConflictException("Email đã được sử dụng.");
                }

                throw;
            }

            return user.Id;
        }
    }

}
