using MediatR;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Mini_Cloud.Application.Features.Files;

public record RenameFileCommand(
    Guid userId,
    Guid FileId,
    string NewFileName
    ) : IRequest<FileDetailDto>;
public class RenameFileCommandHandler : IRequestHandler<RenameFileCommand, FileDetailDto>
{
    private readonly IApplicationDbContext _context;
    public RenameFileCommandHandler( IApplicationDbContext context) {  _context = context; }
    public async Task<FileDetailDto> Handle ( RenameFileCommand request,CancellationToken cancellationToken)
    {
        if (request.userId == Guid.Empty)
        {
            throw new UnauthorizedException();
        }
        var newName = request.NewFileName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(newName) || newName.Length > 255)
        {
            throw new BadRequestException("Tên file mới không được để trống và không quá 255 ký tự.");
        }

        if (newName.Contains('/') || newName.Contains('\\'))
        {
            throw new BadRequestException("Tên file không được chứa ký tự đường dẫn (/ hoặc \\).");
        }
        if (newName.Any(char.IsControl))
        {
            throw new BadRequestException("Tên file không được chứa ký tự điều khiển.");
        }
        var file =await _context.StoredFiles.FirstOrDefaultAsync(f => f.FileName == newName&&f.UserId == request.userId);
        if( file == null )
        {
            throw new NotFoundException("Không tìm thấy file hoặc bạn không có quyền truy cập.");
        }
        file.FileName = newName;
        await _context.SaveChangesAsync(cancellationToken);
        return new FileDetailDto(
            file.Id,
            file.FileName,
            file.FileSize,
            file.CreatedAt,
            file.IsShared,
            file.ShareToken,
            file.ShareExpiresAt
        );

    }
}



    

   

