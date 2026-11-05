
using System.Net;


namespace Mini_Cloud.Application.Common.Exceptions
{
    public abstract class AppException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        protected AppException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message)
            : base(message, HttpStatusCode.BadRequest)
        {
        }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Chưa đăng nhập hoặc phiên làm việc đã hết hạn.")
            : base(message, HttpStatusCode.Unauthorized)
        {
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message = "Không tìm thấy dữ liệu yêu cầu.")
            : base(message, HttpStatusCode.NotFound)
        {
        }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, HttpStatusCode.Conflict)
        {
        }
    }

    public class PayloadTooLargeException : AppException
    {
        public PayloadTooLargeException(string message = "Dung lượng vượt quá giới hạn cho phép.")
            : base(message, HttpStatusCode.RequestEntityTooLarge)
        {
        }
    }

    public class TooManyRequestsException : AppException
    {
        public TooManyRequestsException(string message = "Gọi quá nhiều yêu cầu, vui lòng thử lại sau.")
            : base(message, HttpStatusCode.TooManyRequests)
        {
        }
    }
}
