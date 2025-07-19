using UserManagementApi.Middlewares;
using UserManagementApi.Controllers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();            // ⬅ Global error handler
app.UseMiddleware<TokenAuthenticationMiddleware>();          // ⬅ Auth middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();       // ⬅ Logging middleware

app.MapUserEndpoints();

app.Run();
