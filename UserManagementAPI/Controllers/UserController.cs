using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using UserManagementApi.Models;
using UserManagementApi.Helpers;

namespace UserManagementApi.Controllers;

public static class UserRoutes
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = new List<User>
        {
            new User { Id = 1, Username = "alice", Userage = 25 },
            new User { Id = 2, Username = "bob", Userage = 30 }
        };

        app.MapGet("/users", (int page = 1, int pageSize = 10) => {
            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Results.Ok(new
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)users.Count / pageSize),
                TotalUsers = users.Count,
                Users = pagedUsers
            });
        });

        app.MapGet("/users/simple", () =>
        {
            var simpleList = users.Select(u => new { u.Id, u.Username });
            return Results.Ok(simpleList);
        });

        app.MapGet("/users/{id}", (int id) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            return user is null
                ? Results.NotFound(new { Message = $"User {id} not found." })
                : Results.Ok(user);
        });

        app.MapPost("/users", async (HttpContext context) =>
        {
            var newUser = await context.Request.ReadFromJsonAsync<User>();
            if (!UserValidator.IsValidUser(newUser, out var error))
                return Results.BadRequest(new { Message = error });

            newUser.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
            users.Add(newUser);

            return Results.Created($"/users/{newUser.Id}", newUser);
        });

        app.MapPut("/users/{id}", async (int id, HttpContext context) =>
        {
            var updatedUser = await context.Request.ReadFromJsonAsync<User>();
            var existing = users.FirstOrDefault(u => u.Id == id);
            if (existing is null)
                return Results.NotFound(new { Message = $"User {id} not found." });

            if (!UserValidator.IsValidUser(updatedUser, out var error))
                return Results.BadRequest(new { Message = error });

            existing.Username = updatedUser.Username;
            existing.Userage = updatedUser.Userage;
            return Results.Ok(existing);
        });

        app.MapDelete("/users/{id}", (int id) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user is null)
                return Results.NotFound(new { Message = $"User {id} not found." });

            users.Remove(user);
            return Results.Ok(new { Message = $"User {id} deleted", DeletedUser = user });
        });
    }
}
