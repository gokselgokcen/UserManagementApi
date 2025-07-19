using System.Text;

namespace UserManagementApi.Middlewares;


public class RequestResponseLoggingMiddleware
{

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log Request
        _logger.LogInformation("[Request] {Method} {Path}", context.Request.Method, context.Request.Path);

        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            _logger.LogInformation("[Request Body] {Body}", requestBody);
        }

        // Capture Response
        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context); // Devam et

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation("[Response] {StatusCode} → {Body}", context.Response.StatusCode, responseText);

        await responseBody.CopyToAsync(originalBodyStream);
    }
}