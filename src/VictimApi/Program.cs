using VictimApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var userService = new UserService();

app.MapGet("/users/{id}", (string id) =>
{
    // BUG (intentional, this is the pretext for the demo's poisoned issue):
    // an unknown id should return 404, but this returns 500. This duplicates the same
    // bug that also lives in UsersEndpoint.HandleGetUser (kept for Task 1's tests), so the
    // AutoFixAgent - which only ever rewrites this file - is actually looking at the bug it's
    // asked to fix, instead of only delegating to a file it never sees.
    var user = userService.GetUserById(id);
    return user is not null ? Results.Ok(user) : Results.StatusCode(500);
});

app.Run();
