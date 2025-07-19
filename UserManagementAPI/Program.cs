using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Yanıt zaten başladıysa, middleware’den geri dönemeyiz
        if (context.Response.HasStarted)
        {
            Console.WriteLine("❗ Response already started, can't write error.");
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Internal server error."
            // DEBUG modda ex.Message dahil edilebilir, ancak PROD için önerilmez
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

app.UseMiddleware<TokenAuthenticationMiddleware>(); // ↖ Bu kesinlikle mapping’den önce olmalı

app.UseMiddleware<RequestResponseLoggingMiddleware>();





// In-memory store
var users = new List<User>
{
    new User { Id = 1, Username = "alice", Userage = 25 },
    new User { Id = 2, Username = "bob", Userage = 30 }
};

// GET: List all users "Optimized with Pagination via Copilot"
app.MapGet("/users", (int page = 1, int pageSize = 10) =>
{
    var pagedUsers = users
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    var totalCount = users.Count;
    var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

    return Results.Ok(new
    {
        Page = page,
        PageSize = pageSize,
        TotalPages = totalPages,
        TotalUsers = totalCount,
        Users = pagedUsers
    });
});

// GET: List all users with simple structure for mobiles etc.
app.MapGet("/users/simple", () =>
{
    var simpleList = users.Select(u => new
    {
        u.Id,
        u.Username
    });

    return Results.Ok(simpleList);
});


// GET: Retrieve user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);

    if (user is null)
    {
        return Results.NotFound(new
        {
            Message = $"User with ID {id} was not found.",
            ErrorCode = 404
        });
    }

    return Results.Ok(user);
});

// POST: Add new user
app.MapPost("/users", async (HttpContext context) =>
{
    var newUser = await context.Request.ReadFromJsonAsync<User>();

    if (!UserValidator.IsValidUser(newUser, out var error))
    {
        return Results.BadRequest(new { Message = error });
    }

    newUser.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);

    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT: Update existing user
app.MapPut("/users/{id}", async (int id, HttpContext context) =>
{
    var updateUser = await context.Request.ReadFromJsonAsync<User>();
    var existing = users.FirstOrDefault(u => u.Id == id);

    if (existing is null)
    {
        return Results.NotFound(new { Message = $"User with ID {id} not found." });
    }

    if (!UserValidator.IsValidUser(updateUser, out var error))
    {
        return Results.BadRequest(new { Message = error });
    }

    existing.Username = updateUser.Username;
    existing.Userage = updateUser.Userage;

    return Results.Ok(existing);
});

// DELETE: Remove user by ID
app.MapDelete("/users/{id}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new
        {
            Message = "Invalid user ID. ID must be greater than 0.",
            ErrorCode = 400
        });
    }

    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is not null)
    {
        users.Remove(user);
        return Results.Ok(new
        {
            Message = $"Deleted user with ID {id}",
            DeletedUser = user
        });
    }

    return Results.NotFound(new
    {
        Message = $"User with ID {id} was not found.",
        ErrorCode = 404
    });
});

app.Run();

// Model
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int Userage { get; set; }
}

// Helper function
public static class UserValidator
{
    public static bool IsValidUser(User user, out string error)
    {
        error = "";

        if (user is null)
        {
            error = "User data is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            error = "Username is required.";
            return false;
        }

        if (user.Userage < 0 || user.Userage > 120)
        {
            error = "Userage must be between 0 and 120.";
            return false;
        }

        return true;
    }
}

// Move middleware class to namespace level
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

// TokenAuthenticationMiddleware should be a top-level class, not nested
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