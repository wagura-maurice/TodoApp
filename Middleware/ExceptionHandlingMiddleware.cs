using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TodoApp.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly bool _isDevelopment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
            _isDevelopment = env.IsDevelopment();
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
            var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                        (context.Request.Headers.TryGetValue("Content-Type", out var contentType) && 
                         contentType.ToString().Contains("application/json"));
            
            var errorId = Activity.Current?.Id ?? context.TraceIdentifier;
            
            // Log the exception
            _logger.LogError(exception, "An unhandled exception has occurred. Request ID: {RequestId}", errorId);

            // For AJAX requests, return JSON response
            if (isAjax || context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    success = false,
                    message = "An error occurred while processing your request.",
                    error = _isDevelopment ? exception.Message : null,
                    requestId = errorId,
                    stackTrace = _isDevelopment ? exception.StackTrace : null
                };

                // Set status code based on exception type
                context.Response.StatusCode = exception switch
                {
                    UnauthorizedAccessException _ => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException _ => StatusCodes.Status404NotFound,
                    InvalidOperationException _ => StatusCodes.Status400BadRequest,
                    ArgumentException _ => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            // For non-AJAX requests, use ProblemDetails
            context.Response.ContentType = "application/json";
            
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "An error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path,
                Detail = _isDevelopment ? exception.Message : null,
                Extensions = { ["traceId"] = errorId }
            };

            // Customize based on exception type
            switch (exception)
            {
                case UnauthorizedAccessException _:
                    problemDetails.Status = StatusCodes.Status401Unauthorized;
                    problemDetails.Title = "Unauthorized";
                    problemDetails.Detail = "You are not authorized to access this resource.";
                    break;
                
                case KeyNotFoundException _:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = "Resource not found";
                    problemDetails.Detail = "The requested resource was not found.";
                    break;
                
                case InvalidOperationException _:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Invalid operation";
                    problemDetails.Detail = exception.Message;
                    break;
                
                case ArgumentException _:
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Invalid argument";
                    problemDetails.Detail = exception.Message;
                    break;
            }

            // Set the HTTP status code
            context.Response.StatusCode = problemDetails.Status.Value;

            // Add exception details in development
            if (_isDevelopment)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
            }

            // Return the response as JSON
            var result = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(result);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
