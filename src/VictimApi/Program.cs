using VictimApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var userService = new UserService();

app.MapGet("/users/{id}", (string id) =>
{
    return UsersEndpoint.HandleGetUser(userService, id);
});

app.Run();
