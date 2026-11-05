using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Features.Files;
using System.Security.Claims;

namespace Mini_Cloud.Api.Controllers
{
    public class FilesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FilesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId
        {
            get
            {
                var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userId))
                {
                    throw new UnauthorizedException("Không tìm thấy thông tin danh tính trong token.");
                }
                return userId;
            }
        }

        [Authorize]
        [HttpPost("upload")]
        [RequestSizeLimit(105_000_000)] 
        public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("File không được để rỗng.");
            }

            using var stream = file.OpenReadStream();
            var command = new UploadFileCommand(
                CurrentUserId,
                stream,
                file.FileName,
                file.Length
            );

            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetFiles(
            [FromBody] string? query,
            [FromBody] int page = 1,
            [FromBody] int pagesize=10
            )
        {
            var result= _mediator.Send(new GetFilesQuery(CurrentUserId, query, page, pagesize));
            return Ok(result);
        }
        [Authorize]
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DownloadFileQuery(CurrentUserId, id), cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
        }
        [HttpPatch("{id:guid}/rename")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] RenameFileRequest request,CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrEmpty(request.NewFileName))
                throw new BadRequestException("Tên file mới không được để rỗng.");
            var result = _mediator.Send(new RenameFileCommand(CurrentUserId, id, request.NewFileName));
            return Ok(result);
        }
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteFileCommand(CurrentUserId, id), cancellationToken);
            return Ok(new
            {
                message = "Xóa file thành công.",
                file = result
            });
        }
        [Authorize]
        [HttpPost("{id:guid}/share")]
        public async Task<IActionResult> CreateShareLink(Guid id, [FromBody] ShareFileRequest? request, CancellationToken cancellationToken)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _mediator.Send(new CreateShareLinkCommand(CurrentUserId, id, request?.ExpiresInHours, baseUrl), cancellationToken);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("{id:guid}/revoke-share")]
        [HttpDelete("{id:guid}/share")]
        public async Task<IActionResult> RevokeShareLink(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new RevokeShareLinkCommand(CurrentUserId, id), cancellationToken);
            return Ok(new { message = "Đã thu hồi link chia sẻ thành công." });
        }

        [AllowAnonymous]
        [HttpGet("shared/{token}")]
        public async Task<IActionResult> DownloadSharedFile(string token, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DownloadSharedFileQuery(token), cancellationToken);
            return File(result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
        }
        public record RenameFileRequest(string NewFileName);
        public record ShareFileRequest(int? ExpiresInHours);
    }
}
