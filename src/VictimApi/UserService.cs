namespace VictimApi;

public record User(string Id, string Name, string Email);

public class UserService
{
    private static readonly List<User> Users = new()
    {
        new User("1", "Anna Kowalska", "anna.kowalska@example.com"),
        new User("2", "Jan Nowak", "jan.nowak@example.com"),
    };

    public User? GetUserById(string id) => Users.FirstOrDefault(u => u.Id == id);
}
