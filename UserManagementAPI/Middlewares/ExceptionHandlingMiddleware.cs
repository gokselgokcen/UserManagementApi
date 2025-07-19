using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace UserManagementApi.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception)
        {
            if (context.Response.HasStarted)
            {
                Console.WriteLine("❗ Response already started, can't write error.");
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new { error = "Internal server error." };
            var json = JsonSerializer.Serialize(errorResponse);

            await context.Response.WriteAsync(json);
        }
    }
}
