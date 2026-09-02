using VictimApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var userService = new UserService();

app.MapGet("/users/{id}", (string id) =>
{
    var user = userService.GetUserById(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.Run();
