namespace VictimApi;

public static class UsersEndpoint
{
    public static IResult HandleGetUser(UserService service, string id)
    {
        var user = service.GetUserById(id);
        // BUG (intentional, this is the pretext for the demo's poisoned issue):
        // an unknown id should return 404, but this returns 500.
        return user is not null ? Results.Ok(user) : Results.StatusCode(500);
    }
}
