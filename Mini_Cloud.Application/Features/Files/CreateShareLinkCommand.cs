using MediatR;
using Microsoft.EntityFrameworkCore;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mini_Cloud.Application.Features.Files;

public record CreateShareLinkCommand(
    Guid UserId,
    Guid FileId,
    int? ExpiresInHours = null,
    string? BaseUrl = null
) : IRequest<ShareLinkResponse>;

public record ShareLinkResponse(
    string ShareUrl,
    string ShareToken,
    DateTime? ExpiresAt
);

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, ShareLinkResponse>
{
    private readonly IApplicationDbContext _context;

    public CreateShareLinkCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShareLinkResponse> Handle(CreateShareLinkCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new UnauthorizedException();
        }

        if (request.ExpiresInHours.HasValue)
        {
            if (request.ExpiresInHours.Value < 1 || request.ExpiresInHours.Value > 8760)
            {
                throw new BadRequestException("Thời hạn chia sẻ phải từ 1 giờ đến 8760 giờ (1 năm).");
            }
        }

        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == request.UserId, cancellationToken);

        if (file == null)
        {
            throw new NotFoundException("Không tìm thấy file hoặc bạn không có quyền truy cập.");
        }

        // Generate cryptographically secure 256-bit token (32 random bytes -> 64 hex characters)
        if (!file.IsShared || string.IsNullOrEmpty(file.ShareToken))
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            file.ShareToken = Convert.ToHexString(randomBytes).ToLowerInvariant();
            file.IsShared = true;
        }

        file.ShareExpiresAt = request.ExpiresInHours.HasValue
            ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value)
            : null;

        await _context.SaveChangesAsync(cancellationToken);

        var relativePath = $"/api/files/shared/{file.ShareToken}";
        var fullUrl = !string.IsNullOrWhiteSpace(request.BaseUrl)
            ? $"{request.BaseUrl.TrimEnd('/')}{relativePath}"
            : relativePath;
        return new ShareLinkResponse(
            fullUrl,
            file.ShareToken,
            file.ShareExpiresAt
        );
    }
}





