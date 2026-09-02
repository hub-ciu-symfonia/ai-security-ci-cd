namespace VictimApi;

public static class UsersEndpoint
{
    public static IResult HandleGetUser(UserService service, string id)
    {
        var user = service.GetUserById(id);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
}
