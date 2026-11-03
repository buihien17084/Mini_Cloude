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
        [RequestSizeLimit(105_000_000)] // Allow up to 105MB for multipart payload
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
    }
}
