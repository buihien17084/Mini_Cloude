using Mini_Cloud.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace Mini_Cloud.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        string message;

        if (exception is AppException appEx)
        {
            statusCode = appEx.StatusCode;
            message = appEx.Message;
            _logger.LogWarning("Nghiệp vụ trả về mã {StatusCode}: {Message}", (int)statusCode, message);
        }
        else
        {
            statusCode = HttpStatusCode.InternalServerError;
            message = "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.";
            _logger.LogError(exception, "Lỗi chưa được xử lý phát sinh trong hệ thống: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            statusCode = (int)statusCode,
            message
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}


