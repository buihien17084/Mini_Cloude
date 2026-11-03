using MediatR;
using Microsoft.EntityFrameworkCore;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;


namespace Mini_Cloud.Application.Features.Files;

public record GetFilesQuery(
    Guid UserId,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<FileListDto>>;

public  class GetFilesQueryHandler : IRequestHandler<GetFilesQuery, PagedResult<FileListDto>>
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private readonly IApplicationDbContext _context;
    public GetFilesQueryHandler (IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<PagedResult<FileListDto>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new UnauthorizedException();
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : (request.PageSize > MaxPageSize ? MaxPageSize : request.PageSize);

        var query = _context.StoredFiles
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(f => f.FileName.ToLower().Contains(search));
        }

        var totalFiles = await query.CountAsync(cancellationToken);

        // Map to FileListDto: Excludes ShareToken to avoid exposing secrets in list views
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FileListDto(
                f.Id,
                f.FileName,
                f.FileSize,
                f.CreatedAt,
                f.IsShared
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<FileListDto>(items, totalFiles, page, pageSize);
    }
}


    

   

