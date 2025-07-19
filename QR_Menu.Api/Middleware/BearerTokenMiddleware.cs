using Microsoft.AspNetCore.Http;

namespace QR_Menu.Api.Middleware;

/// <summary>
/// Middleware that automatically prepends "Bearer " to Authorization headers
/// This allows users to paste just the JWT token in Swagger
/// </summary>
public class BearerTokenMiddleware
{
    private readonly RequestDelegate _next;

    public BearerTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if Authorization header exists but doesn't start with "Bearer "
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader) && !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Prepend "Bearer " to the token
            context.Request.Headers["Authorization"] = $"Bearer {authHeader}";
        }

        await _next(context);
    }
} 