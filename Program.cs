var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com" }
};

// Error-handling middleware (first)
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var errorJson = System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error." });
        await context.Response.WriteAsync(errorJson);
        Console.WriteLine($"Unhandled Exception: {ex.Message}");
    }
});

// Authentication middleware (next)
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrEmpty(token) || token != "Bearer demo-token")
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        var errorJson = System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized" });
        await context.Response.WriteAsync(errorJson);
        return;
    }
    await next.Invoke();
});

// Logging middleware (last)
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// GET: Retrieve all users
app.MapGet("/users", () => Results.Ok(users.ToList()));

// GET: Retrieve user by ID
app.MapGet("/users/{id}", (int id, HttpContext context) =>
{
    try
    {  
        var user = users.FirstOrDefault(u => u.Id == id);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving user: {ex.Message}");
    }
});

// POST: Add a new user
app.MapPost("/users", (User user) =>
{
    // Validation
    if (string.IsNullOrWhiteSpace(user.Name))
        return Results.BadRequest("Name is required.");
    if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
        return Results.BadRequest("Valid email is required.");

    user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    // Validation
    if (string.IsNullOrWhiteSpace(updatedUser.Name))
        return Results.BadRequest("Name is required.");
    if (string.IsNullOrWhiteSpace(updatedUser.Email) || !updatedUser.Email.Contains("@"))
        return Results.BadRequest("Valid email is required.");

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", (int id) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null) return Results.NotFound();
        users.Remove(user);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error deleting user: {ex.Message}");
    }
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
