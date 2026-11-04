using MediatR;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mini_Cloud.Application.Features.Files;

public record RevokeShareLinkCommand(
    Guid UserId,
    Guid FileId
) : IRequest<bool>;

public class RevokeShareLinkCommandHandler : IRequestHandler<RevokeShareLinkCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RevokeShareLinkCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RevokeShareLinkCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new UnauthorizedException();
        }

        var file = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == request.UserId, cancellationToken);

        if (file == null)
        {
            throw new NotFoundException("Không tìm thấy file hoặc bạn không có quyền truy cập.");
        }

        file.IsShared = false;
        file.ShareToken = null;
        file.ShareExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}



