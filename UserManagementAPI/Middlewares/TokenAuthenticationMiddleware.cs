namespace UserManagementApi.Middlewares;


public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string SECRET_KEY = "my_secure_key_123"; // örnek sabit key

    public TokenAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid Authorization header.");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Basit doğrulama (gerçek uygulamada JWT doğrulaması yapılmalı)
        if (token != SECRET_KEY)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid token.");
            return;
        }

        // Token geçerliyse bir sonraki middleware'e geç
        await _next(context);
    }
}