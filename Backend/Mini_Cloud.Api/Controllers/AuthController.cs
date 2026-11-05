using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Mini_Cloud.Application.Features.Auth;

namespace Mini_Cloud.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("AuthRateLimit")]
    public class AuthController: ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var command = new RegisterCommand(request.UserName, request.Gmail, request.Password);
            var userId = await _mediator.Send(command, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = userId,
                message = "Đăng ký tài khoản thành công."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var command = new LoginCommand(request.Gmail, request.Password);
            var response = await _mediator.Send(command, cancellationToken);

            return Ok(response);
        }
    }

    public record RegisterRequest(string UserName, string Gmail, string Password);
    public record LoginRequest(string Gmail, string Password);

}
    



