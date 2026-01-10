using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SecureApi.Middleware
{



    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    ValidationException => StatusCodes.Status400BadRequest,
                    //ArgumentException => StatusCodes.Status400BadRequest,
                    //ArgumentNullException => StatusCodes.Status400BadRequest,
                    DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                    DbUpdateException dbEx when IsUniqueConstraintViolation(dbEx) => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status500InternalServerError
                };

                var response = new
                {
                    message = ex switch
                    {
                        DbUpdateException dbEx when IsUniqueConstraintViolation(dbEx) => "Resource already exists.",
                        ValidationException valEx => valEx.Message,
                        KeyNotFoundException keyEx => keyEx.Message,
                        ArgumentException argEx => argEx.Message,
                        //ArgumentNullException nullEx => nullEx.Message,
                        DbUpdateConcurrencyException => "The resource was updated or deleted by another request. Please retry.",
                        _ => "An unexpected error occurred."
                    },
                    details = ex.InnerException?.Message
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }

        // Helper to detect SQL Server unique constraint violation
        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            }
            return false;
        }
    }
}
