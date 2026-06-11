using System.Net;
using System.Text.Json;

namespace SafeTrace.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                var message = _environment.IsDevelopment()
                    ? ex.InnerException is not null
                        ? $"{ex.Message} | {ex.InnerException.Message}"
                        : ex.Message
                    : "An unexpected error occurred.";

                await WriteErrorAsync(context, HttpStatusCode.InternalServerError, message);
            }
        }

        private static async Task WriteErrorAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = (int)statusCode,
                Error = message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
